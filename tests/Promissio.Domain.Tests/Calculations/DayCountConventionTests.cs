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
    private void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
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
    private void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
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
    private void Fraction_WithinSameYear_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
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
    public void Name_ReturnsActualActual()
    {
        var convention = new ActualActual();
        convention.Name.Should().Be("Actual/Actual");
    }

    [Fact]
    public void Fraction_SameDate_ReturnsZero()
    {
        var convention = new ActualActual();
        var date = new LocalDate(2023, 1, 1);

        Decimal fraction = convention.Fraction(date, date);

        fraction.Should().Be(0m);
    }
}

public class Thirty360Tests
{
    [Theory]
    [InlineData(2023, 1, 1, 2023, 2, 1, 30)]
    [InlineData(2023, 1, 1, 2023, 3, 1, 60)]
    [InlineData(2023, 1, 1, 2024, 1, 1, 360)]
    [InlineData(2023, 1, 31, 2023, 2, 1, 1)]
    [InlineData(2023, 1, 1, 2023, 1, 31, 29)]
    [InlineData(2023, 2, 1, 2023, 3, 1, 30)]
    [InlineData(2023, 2, 28, 2023, 3, 31, 32)]
    [InlineData(2024, 2, 1, 2024, 3, 1, 30)]
    [InlineData(2024, 2, 29, 2024, 3, 31, 31)]
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
  private void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
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
  private void Fraction_ReturnsCorrectValue(int startYear, int startMonth, int startDay,
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
