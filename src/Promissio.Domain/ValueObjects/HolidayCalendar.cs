using NodaTime;

namespace Promissio.Domain.ValueObjects;

/// <summary>An immutable set of supplied holidays with Saturday and Sunday closures.</summary>
/// <remarks>No jurisdictional holiday list is inferred; callers supply the applicable calendar.</remarks>
public sealed record HolidayCalendar
{
    private readonly LocalDate[] _holidays;

    /// <summary>The supplied holiday dates, sorted and distinct.</summary>
    public IReadOnlyList<LocalDate> Holidays => Array.AsReadOnly(_holidays);

    /// <summary>Copies the supplied holiday dates.</summary>
    public HolidayCalendar(IEnumerable<LocalDate> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        _holidays = holidays.Distinct().OrderBy(d => d).ToArray();
    }

    /// <summary>Returns whether a date is a weekend or a supplied holiday.</summary>
    public bool IsHoliday(LocalDate date) =>
        date.DayOfWeek is IsoDayOfWeek.Saturday or IsoDayOfWeek.Sunday || _holidays.Contains(date);

    /// <summary>Returns the date itself or the following open business day.</summary>
    public LocalDate NextBusinessDay(LocalDate date)
    {
        while (IsHoliday(date))
            date = date.PlusDays(1);
        return date;
    }

    /// <summary>Returns the date itself or the preceding open business day.</summary>
    public LocalDate PreviousBusinessDay(LocalDate date)
    {
        while (IsHoliday(date))
            date = date.PlusDays(-1);
        return date;
    }

    /// <summary>Returns the nearest open business day, choosing the following day on a tie.</summary>
    public LocalDate NearestBusinessDay(LocalDate date)
    {
        LocalDate next = NextBusinessDay(date);
        LocalDate previous = PreviousBusinessDay(date);
        return Period.Between(previous, date, PeriodUnits.Days).Days < Period.Between(date, next, PeriodUnits.Days).Days
            ? previous : next;
    }

    /// <summary>Compares the supplied holiday sets by value.</summary>
    public bool Equals(HolidayCalendar? other) => other is not null && _holidays.SequenceEqual(other._holidays);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (LocalDate holiday in _holidays)
            hash.Add(holiday);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => $"HolidayCalendar({_holidays.Length} supplied holidays; weekends closed)";
}
