using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Calculations;

/// <summary>
/// Interface for interest calculation operations.
/// </summary>
/// <remarks>
/// This is the single, authoritative path for computing interest in the domain.
/// All interest computation flows through this interface, never inline math.
/// The calculator delegates to InterestRate.CalculateInterest() which encapsulates
/// both the rate logic and day-count convention for each rate type.
/// </remarks>
public interface IInterestCalculator
{
    /// <summary>
    /// Calculates interest for a single period.
    /// </summary>
    /// <param name="principal">The principal amount on which interest is computed.</param>
    /// <param name="rate">The applicable interest rate (encapsulates convention).</param>
    /// <param name="startDate">The start date of the period (inclusive).</param>
    /// <param name="endDate">The end date of the period (exclusive).</param>
    /// <returns>The interest amount for the period.</returns>
    Money Calculate(Money principal, InterestRate rate, LocalDate startDate, LocalDate endDate);

    /// <summary>
    /// Calculates interest for multiple consecutive periods.
    /// </summary>
    /// <param name="principal">The principal amount on which interest is computed.</param>
    /// <param name="rate">The applicable interest rate (encapsulates convention).</param>
    /// <param name="periods">The consecutive periods to compute interest for.</param>
    /// <returns>A list of interest amounts, one per period.</returns>
    IReadOnlyList<Money> CalculateForPeriods(
        Money principal,
        InterestRate rate,
        IReadOnlyList<(LocalDate StartDate, LocalDate EndDate)> periods);
}
