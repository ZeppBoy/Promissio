using System;
using Promissio.Domain.ValueObjects;
using NodaTime;

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
    /// <param name="interestRate">The annual interest rate.</param>
    /// <param name="termMonths">The total term of the loan in months.</param>
    /// <param name="startDate">The date from which the first payment period begins.</param>
    /// <param name="gracePeriodMonths">The number of months of grace period at the start of the loan.</param>
    /// <returns>A collection of payment schedule items.</returns>
    IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        Percentage interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0);
}

public record PaymentScheduleItem(
    int Period,
    LocalDate PaymentDate,
    Money PrincipalPortion,
    Money InterestPortion,
    Money TotalPayment);
