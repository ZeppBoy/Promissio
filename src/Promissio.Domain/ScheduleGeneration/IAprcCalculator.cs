using System.Collections.Generic;
using Promissio.Domain.ValueObjects;
using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Defines the contract for calculating the Annual Percentage Rate of Charge (APRC).
/// </summary>
public interface IAprcCalculator
{
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
