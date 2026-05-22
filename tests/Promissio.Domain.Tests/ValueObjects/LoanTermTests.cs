using FluentAssertions;
using NodaTime;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ValueObjects;

public class LoanTermTests
{
    [Fact]
    public void FromMonths_ValidValue_ReturnsCorrectTerm()
    {
        var term = LoanTerm.FromMonths(12);

        term.TotalMonths.Should().Be(12);
        term.Years.Should().Be(1);
        term.Months.Should().Be(0);
    }

    [Fact]
    public void FromMonths_PartialYear_ReturnsCorrectBreakdown()
    {
        var term = LoanTerm.FromMonths(18);

        term.TotalMonths.Should().Be(18);
        term.Years.Should().Be(1);
        term.Months.Should().Be(6);
    }

    [Fact]
    public void FromYears_ValidValue_ReturnsCorrectTerm()
    {
        var term = LoanTerm.FromYears(5);

        term.TotalMonths.Should().Be(60);
        term.Years.Should().Be(5);
        term.Months.Should().Be(0);
    }

    [Fact]
    public void FromMonths_Zero_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => LoanTerm.FromMonths(0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromMonths_Negative_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => LoanTerm.FromMonths(-12);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EndDate_CalculatesCorrectEndDate()
    {
        var term = LoanTerm.FromMonths(12);
        var startDate = new LocalDate(2024, 1, 15);

        var endDate = term.EndDate(startDate);

        endDate.Should().Be(new LocalDate(2025, 1, 15));
    }

    [Fact]
    public void EndDate_HandlesMonthBoundaries()
    {
        var term = LoanTerm.FromMonths(3);
        var startDate = new LocalDate(2024, 1, 31);

        var endDate = term.EndDate(startDate);

        endDate.Should().Be(new LocalDate(2024, 4, 30));
    }

    [Fact]
    public void Equality_SameTotalMonths_AreEqual()
    {
        var a = LoanTerm.FromMonths(12);
        var b = LoanTerm.FromYears(1);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var term = LoanTerm.FromMonths(26);

        term.ToString().Should().Be("LoanTerm(2y 2m, 26 months total)");
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquality()
    {
        var a = LoanTerm.FromMonths(12);
        var b = LoanTerm.FromYears(1);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
