using FluentAssertions;
using NodaTime;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ValueObjects;

public sealed class HolidayCalendarTests
{
    [Fact]
    public void Calendar_IsImmutableAndHasSetEquality()
    {
        LocalDate holiday = new(2024, 6, 3);
        List<LocalDate> dates = [holiday, holiday];
        HolidayCalendar calendar = new(dates);
        dates.Clear();
        calendar.Should().Be(new HolidayCalendar([holiday]));
        calendar.GetHashCode().Should().Be(new HolidayCalendar([holiday]).GetHashCode());
        calendar.IsHoliday(holiday).Should().BeTrue();
        calendar.Holidays.Should().Equal(holiday);
    }

    [Fact]
    public void Adjustments_RespectWeekendsAndConsecutiveHolidays()
    {
        HolidayCalendar calendar = new([new LocalDate(2024, 6, 3)]);
        calendar.NextBusinessDay(new LocalDate(2024, 6, 1)).Should().Be(new LocalDate(2024, 6, 4));
        calendar.PreviousBusinessDay(new LocalDate(2024, 6, 3)).Should().Be(new LocalDate(2024, 5, 31));
        calendar.NearestBusinessDay(new LocalDate(2024, 6, 1)).Should().Be(new LocalDate(2024, 5, 31));
        calendar.NearestBusinessDay(new LocalDate(2024, 6, 4)).Should().Be(new LocalDate(2024, 6, 4));
    }
}
