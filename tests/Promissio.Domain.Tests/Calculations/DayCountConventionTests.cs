using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;
using Xunit;

namespace Promissio.Domain.Tests.Calculations.DayCounts;

public class Actual360Tests
{
    [Theory]
    [InlineData(2023, 1, 1, 2023, 1, 31, 30)]
    [InlineData(2023, 1, 1, 2023, 2, 1, 31)]
    [InlineData(2023, 6, 1, 2023, 6, 30, 29)]
    [InlineData(2023, 1, 1, 2024, 1, 1, 365)]
    [InlineData(2024, 1, 1, 2025, 1, 1, 366)]
    [InlineData(2023, 3, 1, 2023, 9, 1, 184)]
    [InlineData(2023, 1, 15, 2023, 7, 15, 181)]
    [InlineData(2023, 12, 31, 2024, 1, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 3, 31, 89)]
    [InlineData(2023, 2, 1, 2023, 8, 1, 181)]
    [InlineData(2023, 4, 1, 2023, 10, 1, 183)]
    [InlineData(2023, 7, 1, 2023, 12, 31, 183)]
    [InlineData(2023, 1, 1, 2023, 4, 30, 119)]
    [InlineData(2023, 2, 15, 2023, 8, 15, 181)]
    [InlineData(2023, 5, 1, 2023, 11, 1, 184)]
    [InlineData(2023, 6, 15, 2023, 12, 15, 183)]
    [InlineData(2023, 9, 1, 2024, 3, 1, 182)]
    [InlineData(2023, 10, 15, 2024, 4, 15, 183)]
    [InlineData(2023, 11, 30, 2024, 5, 30, 182)]
    [InlineData(2023, 3, 15, 2023, 9, 15, 184)]
    [InlineData(2024, 2, 28, 2024, 3, 1, 2)]
    [InlineData(2024, 2, 29, 2024, 3, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 1, 2, 1)]
    public void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay, int expectedDays)
    {
        var convention = new Actual360();
        var startDate = new LocalDate(startYear, startMonth, startDay);
        var endDate = new LocalDate(endYear, endMonth, endDay);

        Decimal fraction = convention.Fraction(startDate, endDate);

        Decimal expected = expectedDays / 360m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Name_ReturnsActual360()
    {
        var convention = new Actual360();
        convention.Name.Should().Be("Actual/360");
    }

    [Fact]
    public void Fraction_SameDate_ReturnsZero()
    {
        var convention = new Actual360();
        var date = new LocalDate(2023, 1, 1);

        Decimal fraction = convention.Fraction(date, date);

        fraction.Should().Be(0m);
    }

    [Fact]
    public void Days_ReturnsCorrectCalendarDays()
    {
        var convention = new Actual360();
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 1, 31);

        int days = convention.Days(startDate, endDate);

        days.Should().Be(30);
    }
}

public class Actual365Tests
{
    [Theory]
    [InlineData(2023, 1, 1, 2023, 1, 31, 30)]
    [InlineData(2023, 1, 1, 2023, 2, 1, 31)]
    [InlineData(2023, 6, 1, 2023, 6, 30, 29)]
    [InlineData(2023, 1, 1, 2024, 1, 1, 365)]
    [InlineData(2024, 1, 1, 2025, 1, 1, 366)]
    [InlineData(2023, 3, 1, 2023, 9, 1, 184)]
    [InlineData(2023, 1, 15, 2023, 7, 15, 181)]
    [InlineData(2023, 12, 31, 2024, 1, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 3, 31, 89)]
    [InlineData(2023, 2, 1, 2023, 8, 1, 181)]
    [InlineData(2023, 4, 1, 2023, 10, 1, 183)]
    [InlineData(2023, 7, 1, 2023, 12, 31, 183)]
    [InlineData(2023, 1, 1, 2023, 4, 30, 119)]
    [InlineData(2023, 2, 15, 2023, 8, 15, 181)]
    [InlineData(2023, 5, 1, 2023, 11, 1, 184)]
    [InlineData(2023, 6, 15, 2023, 12, 15, 183)]
    [InlineData(2023, 9, 1, 2024, 3, 1, 182)]
    [InlineData(2023, 10, 15, 2024, 4, 15, 183)]
    [InlineData(2023, 11, 30, 2024, 5, 30, 182)]
    [InlineData(2023, 3, 15, 2023, 9, 15, 184)]
    [InlineData(2024, 2, 28, 2024, 3, 1, 2)]
    [InlineData(2024, 2, 29, 2024, 3, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 1, 2, 1)]
    public void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay, int expectedDays)
    {
        var convention = new Actual365();
        var startDate = new LocalDate(startYear, startMonth, startDay);
        var endDate = new LocalDate(endYear, endMonth, endDay);

        Decimal fraction = convention.Fraction(startDate, endDate);

        Decimal expected = expectedDays / 365m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Name_ReturnsActual365()
    {
        var convention = new Actual365();
        convention.Name.Should().Be("Actual/365");
    }

    [Fact]
    public void Fraction_SameDate_ReturnsZero()
    {
        var convention = new Actual365();
        var date = new LocalDate(2023, 1, 1);

        Decimal fraction = convention.Fraction(date, date);

        fraction.Should().Be(0m);
    }
}

public class ActualActualTests
{
    [Fact]
    public void Fraction_ExactlyOneYearEndsOnAnniversary_KillsClampingMutant()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2023, 6, 15);
        var endDate = new LocalDate(2024, 6, 15);

        Decimal fraction = convention.Fraction(startDate, endDate);

        int totalDays = (endDate.ToDateTimeUnspecified() - startDate.ToDateTimeUnspecified()).Days;
        totalDays.Should().Be(366);
        Decimal expected = 366m / 365m;
        fraction.Should().Be(expected);
    }

    [Theory]
    [InlineData(2023, 1, 1, 2023, 1, 31, 30)]
    [InlineData(2023, 1, 1, 2023, 2, 1, 31)]
    [InlineData(2023, 6, 1, 2023, 6, 30, 29)]
    [InlineData(2023, 3, 1, 2023, 9, 1, 184)]
    [InlineData(2023, 1, 15, 2023, 7, 15, 181)]
    [InlineData(2023, 1, 1, 2023, 3, 31, 89)]
    [InlineData(2023, 2, 1, 2023, 8, 1, 181)]
    [InlineData(2023, 4, 1, 2023, 10, 1, 183)]
    [InlineData(2023, 7, 1, 2023, 12, 31, 183)]
    [InlineData(2023, 1, 1, 2023, 4, 30, 119)]
    [InlineData(2023, 2, 15, 2023, 8, 15, 181)]
    [InlineData(2023, 5, 1, 2023, 11, 1, 184)]
    [InlineData(2023, 6, 15, 2023, 12, 15, 183)]
    [InlineData(2023, 3, 15, 2023, 9, 15, 184)]
    [InlineData(2024, 2, 28, 2024, 3, 1, 2)]
    [InlineData(2024, 2, 29, 2024, 3, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 1, 2, 1)]
    public void Fraction_WithinSameYear_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
         int endYear, int endMonth, int endDay, int expectedDays)
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(startYear, startMonth, startDay);
        var endDate = new LocalDate(endYear, endMonth, endDay);

        Decimal fraction = convention.Fraction(startDate, endDate);

        bool isLeapYear = (startYear % 4 == 0 && startYear % 100 != 0) || startYear % 400 == 0;
        int yearDays = isLeapYear ? 366 : 365;
        Decimal expected = expectedDays / (Decimal)yearDays;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Fraction_CrossYear_2023_2024_ReturnsPinnedValue()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2024, 1, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        fraction.Should().Be(365m / 365m);
    }

    [Fact]
    public void Fraction_CrossYear_2024_2025_ReturnsPinnedValue()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2024, 1, 1);
        var endDate = new LocalDate(2025, 1, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        fraction.Should().Be(366m / 366m);
    }

    [Fact]
    public void Fraction_CrossYear_PartialSegments_ReturnsPinnedValue()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2023, 12, 31);
        var endDate = new LocalDate(2024, 1, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        fraction.Should().Be(1m / 365m);
    }

    [Fact]
    public void Fraction_CrossYear_2023_2024_Partial_ReturnsPinnedValue()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2023, 9, 1);
        var endDate = new LocalDate(2024, 3, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        fraction.Should().Be(182m / 365m);
    }

    [Fact]
    public void Fraction_CrossYear_2023_2024_MultipleSegments_ReturnsPinnedValue()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2023, 6, 15);
        var endDate = new LocalDate(2024, 6, 15);

        Decimal fraction = convention.Fraction(startDate, endDate);

        fraction.Should().Be(366m / 365m);
    }

    [Fact]
    public void Fraction_CenturyYears_2100_NotLeap()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2099, 12, 31);
        var endDate = new LocalDate(2101, 1, 1);

        bool isLeap2100 = (2100 % 4 == 0 && 2100 % 100 != 0) || 2100 % 400 == 0;
        isLeap2100.Should().BeFalse();
    }

    [Fact]
    public void Fraction_CenturyYears_1900_NotLeap()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(1899, 12, 31);
        var endDate = new LocalDate(1901, 1, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        bool isLeap1900 = (1900 % 4 == 0 && 1900 % 100 != 0) || 1900 % 400 == 0;
        isLeap1900.Should().BeFalse();
        Decimal expected = 365m / 365m + 1m / 365m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Fraction_CenturyYears_2100_NotLeap_CrossesCentury()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2099, 12, 31);
        var endDate = new LocalDate(2101, 1, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        bool isLeap2100 = (2100 % 4 == 0 && 2100 % 100 != 0) || 2100 % 400 == 0;
        isLeap2100.Should().BeFalse();
        Decimal expected = 365m / 365m + 1m / 365m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Fraction_ExactlyOneYearBoundary_AnniversaryEqualsEndDate()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2023, 6, 15);
        var endDate = new LocalDate(2024, 6, 15);

        Decimal fraction = convention.Fraction(startDate, endDate);

        int totalDays = (endDate.ToDateTimeUnspecified() - startDate.ToDateTimeUnspecified()).Days;
        totalDays.Should().Be(366);
        Decimal expected = 366m / 365m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Fraction_CenturyYear_DivisibleBy400_IsLeap()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(2000, 2, 28);
        var endDate = new LocalDate(2000, 3, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        bool isLeap2000 = (2000 % 4 == 0 && 2000 % 100 != 0) || 2000 % 400 == 0;
        isLeap2000.Should().BeTrue();
        fraction.Should().Be(2m / 366m);
    }

    [Fact]
    public void Fraction_CenturyYear_DivisibleBy100_NotDivisibleBy400_IsNotLeap()
    {
        var convention = new ActualActual();
        var startDate = new LocalDate(1900, 2, 28);
        var endDate = new LocalDate(1900, 3, 1);

        Decimal fraction = convention.Fraction(startDate, endDate);

        bool isLeap1900 = (1900 % 4 == 0 && 1900 % 100 != 0) || 1900 % 400 == 0;
        isLeap1900.Should().BeFalse();
        int daysInYear = 365;
        fraction.Should().Be(1m / (Decimal)daysInYear);
    }
}

public class Thirty360Tests
{
    [Theory]
    [InlineData(2023, 1, 1, 2023, 2, 1, 30)]
    [InlineData(2023, 1, 1, 2023, 3, 1, 60)]
    [InlineData(2023, 1, 1, 2024, 1, 1, 360)]
    [InlineData(2023, 1, 31, 2023, 2, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 1, 31, 30)]
    [InlineData(2023, 2, 1, 2023, 3, 1, 30)]
    [InlineData(2023, 2, 28, 2023, 3, 31, 33)]
    [InlineData(2024, 2, 1, 2024, 3, 1, 30)]
    [InlineData(2024, 2, 29, 2024, 3, 31, 32)]
    [InlineData(2023, 3, 31, 2023, 4, 30, 30)]
    [InlineData(2023, 1, 15, 2023, 7, 15, 180)]
    [InlineData(2023, 4, 30, 2023, 10, 30, 180)]
    [InlineData(2023, 5, 31, 2023, 11, 30, 180)]
    [InlineData(2023, 7, 1, 2024, 7, 1, 360)]
    [InlineData(2023, 1, 1, 2025, 1, 1, 720)]
    [InlineData(2023, 3, 15, 2023, 9, 15, 180)]
    [InlineData(2023, 6, 1, 2023, 12, 1, 180)]
    [InlineData(2023, 8, 31, 2024, 2, 29, 179)]
    [InlineData(2023, 10, 1, 2024, 4, 1, 180)]
    [InlineData(2023, 11, 30, 2024, 5, 30, 180)]
    [InlineData(2023, 12, 31, 2024, 6, 30, 180)]
    [InlineData(2023, 1, 1, 2023, 4, 15, 104)]
    [InlineData(2023, 4, 15, 2023, 7, 1, 76)]
    [InlineData(2023, 1, 31, 2023, 3, 31, 60)]
    public void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay, int expectedDays)
    {
        var convention = new Thirty360();
        var startDate = new LocalDate(startYear, startMonth, startDay);
        var endDate = new LocalDate(endYear, endMonth, endDay);

        Decimal fraction = convention.Fraction(startDate, endDate);

        Decimal expected = expectedDays / 360m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Name_ReturnsThirty360()
    {
        var convention = new Thirty360();
        convention.Name.Should().Be("30/360");
    }

    [Fact]
    public void Fraction_SameDate_ReturnsZero()
    {
        var convention = new Thirty360();
        var date = new LocalDate(2023, 1, 1);

        Decimal fraction = convention.Fraction(date, date);

        fraction.Should().Be(0m);
    }

    [Fact]
    public void Fraction_D2_31_Boundary_WhenOtherDateDayIsExactly30()
    {
        var convention = new Thirty360();
        var startDate = new LocalDate(2023, 1, 30);
        var endDate = new LocalDate(2023, 3, 31);

        Decimal fraction = convention.Fraction(startDate, endDate);

        int adjustedDay1 = 30;
        int adjustedDay2 = 30;
        int expectedDays = (2023 - 2023) * 360 + (3 - 1) * 30 + (adjustedDay2 - adjustedDay1);
        Decimal expected = expectedDays / 360m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Fraction_D2_31_Boundary_WhenOtherDateDayIsLessThan30()
    {
        var convention = new Thirty360();
        var startDate = new LocalDate(2023, 1, 15);
        var endDate = new LocalDate(2023, 5, 31);

        Decimal fraction = convention.Fraction(startDate, endDate);

        // endDate.Day = 31, startDate.Day = 15. Since otherDate.Day (15) < 30, D2 stays 31.
        int adjustedDay1 = 15;
        int adjustedDay2 = 31;
        int expectedDays = (2023 - 2023) * 360 + (5 - 1) * 30 + (adjustedDay2 - adjustedDay1);
        Decimal expected = expectedDays / 360m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Fraction_D2_31_Boundary_WhenOtherDateDayIsGreaterThanOrEqual30()
    {
        var convention = new Thirty360();
        var startDate = new LocalDate(2023, 1, 31);
        var endDate = new LocalDate(2023, 3, 30);

        Decimal fraction = convention.Fraction(startDate, endDate);

        int adjustedDay1 = 30;
        int adjustedDay2 = 30;
        int expectedDays = (2023 - 2023) * 360 + (3 - 1) * 30 + (adjustedDay2 - adjustedDay1);
        Decimal expected = expectedDays / 360m;
        fraction.Should().Be(expected);
    }

}

public class Thirty360EuropeanTests
{
    [Theory]
    [InlineData(2023, 1, 1, 2023, 2, 1, 30)]
    [InlineData(2023, 1, 1, 2023, 3, 1, 60)]
    [InlineData(2023, 1, 1, 2024, 1, 1, 360)]
    [InlineData(2023, 1, 31, 2023, 2, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 1, 31, 29)]
    [InlineData(2023, 2, 1, 2023, 3, 1, 30)]
    [InlineData(2023, 2, 28, 2023, 3, 1, 3)]
    [InlineData(2024, 2, 1, 2024, 3, 1, 30)]
    [InlineData(2024, 2, 29, 2024, 3, 1, 2)]
    [InlineData(2023, 3, 31, 2023, 4, 1, 1)]
    [InlineData(2023, 1, 15, 2023, 7, 15, 180)]
    [InlineData(2023, 4, 30, 2023, 10, 30, 180)]
    [InlineData(2023, 5, 31, 2023, 11, 30, 180)]
    [InlineData(2023, 7, 1, 2024, 7, 1, 360)]
    [InlineData(2023, 1, 1, 2025, 1, 1, 720)]
    [InlineData(2023, 3, 15, 2023, 9, 15, 180)]
    [InlineData(2023, 6, 1, 2023, 12, 1, 180)]
    [InlineData(2023, 8, 31, 2024, 2, 1, 151)]
    [InlineData(2023, 10, 1, 2024, 4, 1, 180)]
    [InlineData(2023, 11, 30, 2024, 5, 30, 180)]
    [InlineData(2023, 12, 31, 2024, 6, 1, 151)]
    [InlineData(2023, 1, 1, 2023, 4, 15, 104)]
    [InlineData(2023, 4, 15, 2023, 7, 1, 76)]
    public void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay, int expectedDays)
    {
        var convention = new Thirty360European();
        var startDate = new LocalDate(startYear, startMonth, startDay);
        var endDate = new LocalDate(endYear, endMonth, endDay);

        Decimal fraction = convention.Fraction(startDate, endDate);

        Decimal expected = expectedDays / 360m;
        fraction.Should().Be(expected);
    }

    [Fact]
    public void Name_ReturnsThirty360European()
    {
        var convention = new Thirty360European();
        convention.Name.Should().Be("30E/360");
    }

    [Fact]
    public void Fraction_SameDate_ReturnsZero()
    {
        var convention = new Thirty360European();
        var date = new LocalDate(2023, 1, 1);

        Decimal fraction = convention.Fraction(date, date);

        fraction.Should().Be(0m);
    }
}
