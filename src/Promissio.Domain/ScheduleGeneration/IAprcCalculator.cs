using System.Collections.Generic;
using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Defines the contract for calculating the Annual Percentage Rate of Charge (APRC).
/// </summary>
public interface IAprcCalculator
{
    /// <summary>Calculates a dated APRC or returns an explicit input, representability or convergence failure.</summary>
    /// <param name="principal">The single drawdown actually made available to the borrower.</param>
    /// <param name="schedule">Known payments including any charges represented in their totals.</param>
    /// <param name="disbursementDate">Date of the single drawdown.</param>
    /// <param name="maxIterations">Maximum number of bisection iterations.</param>
    /// <returns>A value on success, or a failure explanation.</returns>
    AprcCalculationResult TryCalculate(Money principal, IEnumerable<PaymentScheduleItem> schedule,
        LocalDate disbursementDate, int maxIterations = 100);

    /// <summary>
    /// Calculates the APRC for a loan based on its payment schedule.
    /// </summary>
    /// <param name="principal">The initial principal amount.</param>
    /// <param name="schedule">The actual payment schedule.</param>
    /// <param name="disbursementDate">The date the loan was disbursed.</param>
    /// <param name="maxIterations">Maximum number of iterations for the solver.</param>
    /// <returns>The APRC as a percentage.</returns>
    Percentage Calculate(
        Money principal,
        IEnumerable<PaymentScheduleItem> schedule,
        LocalDate disbursementDate,
        int maxIterations = 100);
}
