using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Calculations;

/// <summary>
/// Implementation of the interest calculation engine.
/// </summary>
/// <remarks>
/// Delegates to InterestRate.CalculateInterest() which encapsulates
/// both the rate logic and day-count convention for each rate type.
/// This ensures TieredRate, FloatingRate, etc. use their correct internal logic.
/// </remarks>
public sealed class InterestCalculator : IInterestCalculator
{
    /// <summary>
    /// Calculates interest for a single period.
    /// </summary>
    public Money Calculate(Money principal, InterestRate rate, LocalDate startDate, LocalDate endDate)
    {
        if (startDate > endDate)
            throw new ArgumentOutOfRangeException(nameof(startDate), "Start date must not be after end date.");

        if (startDate == endDate)
            return Money.Zero(principal.Currency);

        return rate.CalculateInterest(principal, startDate, endDate);
    }

    /// <summary>
    /// Calculates interest for multiple consecutive periods.
    /// </summary>
    public IReadOnlyList<Money> CalculateForPeriods(
        Money principal,
        InterestRate rate,
        IReadOnlyList<(LocalDate StartDate, LocalDate EndDate)> periods)
    {
        var results = new List<Money>(periods.Count);

        foreach (var (startDate, endDate) in periods)
        {
            Money periodInterest = Calculate(principal, rate, startDate, endDate);
            results.Add(periodInterest);
        }

        return results;
    }
}
