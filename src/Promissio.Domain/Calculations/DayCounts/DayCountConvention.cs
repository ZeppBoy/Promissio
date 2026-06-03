using NodaTime;

namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// Base interface for day-count conventions used in interest calculations.
/// </summary>
/// <remarks>
/// Day-count conventions define how to convert calendar days into a fraction of a year
/// for interest computation. Different conventions are used across markets and products.
/// See /docs/domain/day-count-conventions.md for mathematical formulas.
/// </remarks>
public abstract class DayCountConvention : IEquatable<DayCountConvention>
{
    public abstract string Name { get; }

    /// <summary>
    /// Calculates the day-count fraction between two dates.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (exclusive).</param>
    /// <returns>Fraction of a year as a decimal value.</returns>
    public abstract Decimal Fraction(LocalDate startDate, LocalDate endDate);

    /// <summary>
    /// Calculates the actual number of calendar days between two dates.
    /// </summary>
    public virtual int Days(LocalDate startDate, LocalDate endDate) =>
        Period.Between(startDate, endDate, PeriodUnits.Days).Days;

    public bool Equals(DayCountConvention? other) => other != null && this.Name == other.Name;

    public override bool Equals(object? obj) => Equals(obj as DayCountConvention);

    public override int GetHashCode() => HashCode.Combine(Name);

    public override string ToString() => Name;
}
