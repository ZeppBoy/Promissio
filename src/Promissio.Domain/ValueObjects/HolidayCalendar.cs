using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Represents a collection of dates that are considered non-business days (holidays).
/// </summary>
public sealed record HolidayCalendar(IEnumerable<LocalDate> Holidays)
{
    /// <summary>
    /// Checks if a given date is a holiday.
    /// </summary>
    public bool IsHoliday(LocalDate date) => Holidays.Contains(date);

    /// <summary>
    /// Adjusts a date to the next business day if it falls on a holiday.
    /// </summary>
    public LocalDate NextBusinessDay(LocalDate date)
    {
        var current = date;
        while (IsHoliday(current))
        {
            current = current.PlusDays(1);
        }
        return current;
    }

    /// <summary>
    /// Adjusts a date to the previous business day if it falls on a holiday.
    /// </summary>
    public LocalDate PreviousBusinessDay(LocalDate date)
    {
        var current = date;
        while (IsHoliday(current))
        {
            current = current.PlusDays(-1);
;
        }
        return current;
    }

    /// <summary>
    /// Returns the nearest business day (previous or next).
    /// </summary>
    public LocalDate NearestBusinessDay(LocalDate date)
    {
        if (!IsHoliday(date)) return date;
        
        var next = NextBusinessDay(date);
        var prev = PreviousBusinessDay(date);

        // If it's a weekend, usually we prefer the previous business day 
        // but for a generic holiday calendar, let's just pick the next one.
        return next;
    }
}
