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

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("months");
    }

    [Fact]
    public void FromYears_Zero_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => LoanTerm.FromYears(0);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("years");
    }

    [Fact]
    public void FromYears_Negative_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => LoanTerm.FromYears(-1);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("years");
    }

    [Fact]
    public void ToString_SingleYear_ReturnsCorrectFormat()
    {
        var term = LoanTerm.FromMonths(12);

        term.ToString().Should().Be("LoanTerm(1y 0m, 12 months total)");
    }

    [Fact]
    public void ToString_MultipleYearsAndMonths_ContainsYearAndMonth()
    {
        var term = LoanTerm.FromMonths(26);

        term.ToString().Should().Be("LoanTerm(2y 2m, 26 months total)");
    }

    [Fact]
    public void FromMonths_Negative_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => LoanTerm.FromMonths(-12);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("months");
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
    public void Equality_DifferentTotalMonths_AreNotEqual()
    {
        var a = LoanTerm.FromMonths(12);
        var b = LoanTerm.FromMonths(24);

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
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

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var term = LoanTerm.FromMonths(12);

        term.Equals((LoanTerm)null!).Should().BeFalse();
    }

    [Fact]
    public void Equality_Null_LeftSide_ReturnsFalse()
    {
        LoanTerm? a = null;
        LoanTerm b = LoanTerm.FromMonths(12);

        (a == b).Should().BeFalse();
        (b == a).Should().BeFalse();
    }

    [Fact]
    public void Inequality_BothNull_ReturnsTrue()
    {
        LoanTerm? a = null;
        LoanTerm? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void FromMonths_Zero_MessageContainsParameterName()
    {
        Action action = () => LoanTerm.FromMonths(0);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("months");
    }

    [Fact]
    public void FromYears_Zero_MessageContainsParameterName()
    {
        Action action = () => LoanTerm.FromYears(0);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("years");
    }

    [Fact]
    public void ToString_ContainsYearAndMonthComponents()
    {
        var term = LoanTerm.FromMonths(26);

        string str = term.ToString();

        str.Should().Contain("2y");
        str.Should().Contain("2m");
        str.Should().Contain("26 months total");
    }

    [Fact]
    public void FromMonths_Zero_MessageContainsExactText()
    {
        Action action = () => LoanTerm.FromMonths(0);

        action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Loan term must be positive*");
    }

    [Fact]
    public void FromYears_Zero_MessageContainsExactText()
    {
        Action action = () => LoanTerm.FromYears(0);

        action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Loan term must be positive*");
    }
}
