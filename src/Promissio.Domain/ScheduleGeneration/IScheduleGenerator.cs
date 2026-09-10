using System;
using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Defines the contract for generating a payment schedule for a loan.
/// </summary>
public interface IScheduleGenerator
{
    /// <summary>
    /// Generates a payment schedule based on the provided loan parameters.
    /// </summary>
    /// <param name="principal">The initial principal amount of the loan.</param>
    /// <param name="interestRate">The interest rate (encapsulates rate and day-count convention).</param>
    /// <param name="termMonths">The number of monthly payments, including grace instalments.</param>
    /// <param name="startDate">The date from which the first payment period begins.</param>
    /// <param name="gracePeriodMonths">The number of months of grace period at the start of the loan.</param>
    /// <param name="holidayCalendar">Optional calendar for business day adjustments.</param>
    /// <param name="firstPaymentDate">Optional first contractual due date for a short or long first period.</param>
    /// <returns>A collection of payment schedule items.</returns>
    IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        InterestRate interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0,
        HolidayCalendar? holidayCalendar = null,
        LocalDate? firstPaymentDate = null);
}

/// <summary>
/// Represents a single payment in a loan schedule.
/// </summary>
/// <remarks>
/// Enforces invariants:
/// - PrincipalPortion and InterestPortion must be non-negative.
/// - TotalPayment must equal PrincipalPortion + InterestPortion (within rounding tolerance).
/// </remarks>
public sealed record PaymentScheduleItem
{
    /// <summary>The one-based instalment number.</summary>
    public int Period { get; }
    /// <summary>The actual payable date, including any business-day adjustment.</summary>
    public LocalDate PaymentDate { get; }
    /// <summary>The unadjusted due date used to end the interest accrual period.</summary>
    public LocalDate ContractualDate { get; }
    /// <summary>The principal repaid by this instalment.</summary>
    public Money PrincipalPortion { get; }
    /// <summary>The interest due in this instalment.</summary>
    public Money InterestPortion { get; }
    /// <summary>The total due in the same currency as its components.</summary>
    public Money TotalPayment { get; }

    /// <summary>
    /// Creates a new PaymentScheduleItem with validation.
    /// </summary>
    public PaymentScheduleItem(int period, LocalDate paymentDate, Money principalPortion, Money interestPortion,
        Money totalPayment, LocalDate? contractualDate = null)
    {
        ArgumentNullException.ThrowIfNull(principalPortion);
        ArgumentNullException.ThrowIfNull(interestPortion);
        ArgumentNullException.ThrowIfNull(totalPayment);
        if (principalPortion.Currency != interestPortion.Currency || principalPortion.Currency != totalPayment.Currency)
            throw new ArgumentException("All payment components must use the same currency.");
        if (period <= 0)
            throw new ArgumentException("Period must be positive.", nameof(period));

        Period = period;
        PaymentDate = paymentDate;
        ContractualDate = contractualDate ?? paymentDate;
        PrincipalPortion = principalPortion;
        InterestPortion = interestPortion;
        TotalPayment = totalPayment;

        // Validate invariants
        if (PrincipalPortion.Amount < 0)
            throw new ArgumentException("Principal portion must be non-negative.", nameof(principalPortion));

        if (InterestPortion.Amount < 0)
            throw new ArgumentException("Interest portion must be non-negative.", nameof(interestPortion));

        if (TotalPayment.Amount < 0)
            throw new ArgumentException("Total payment must be non-negative.", nameof(totalPayment));

        // TotalPayment should equal PrincipalPortion + InterestPortion (within rounding tolerance)
        var expectedTotal = (PrincipalPortion + InterestPortion).Amount;
        var actualTotal = TotalPayment.Amount;
        if (Math.Abs(expectedTotal - actualTotal) > 0.01m)
        {
            throw new ArgumentException(
                $"Total payment ({actualTotal}) must equal PrincipalPortion + InterestPortion ({expectedTotal}) within rounding tolerance.",
                nameof(totalPayment));
        }
    }

    /// <summary>
    /// Deconstruct for pattern matching.
    /// </summary>
    public void Deconstruct(out int period, out LocalDate paymentDate, out Money principalPortion, out Money interestPortion, out Money totalPayment)
    {
        period = Period;
        paymentDate = PaymentDate;
        principalPortion = PrincipalPortion;
        interestPortion = InterestPortion;
        totalPayment = TotalPayment;
    }
}
