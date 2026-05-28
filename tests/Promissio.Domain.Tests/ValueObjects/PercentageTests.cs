using FluentAssertions;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ValueObjects;

public class PercentageTests
{
    [Fact]
    public void FromPercent_ValidValue_ReturnsCorrectFraction()
    {
        var percentage = Percentage.FromPercent(5m);

        percentage.Fraction.Should().Be(0.05m);
        percentage.AsPercent.Should().Be(5m);
    }

    [Fact]
    public void FromBasisPoints_ValidValue_ReturnsCorrectFraction()
    {
        var percentage = Percentage.FromBasisPoints(500);

        percentage.Fraction.Should().Be(0.05m);
        percentage.AsBasisPoints.Should().Be(500);
    }

    [Fact]
    public void FromFraction_ValidValue_ReturnsCorrectFraction()
    {
        var percentage = Percentage.FromFraction(0.25m);

        percentage.Fraction.Should().Be(0.25m);
        percentage.AsPercent.Should().Be(25m);
    }

    [Fact]
    public void FromPercent_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => Percentage.FromPercent(-1m);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("percent");
    }

    [Fact]
    public void FromPercent_Above100Percent_AllowsValue()
    {
        var percentage = Percentage.FromPercent(150m);

        percentage.AsPercent.Should().Be(150m);
    }

    [Fact]
    public void FromBasisPoints_Negative_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => Percentage.FromBasisPoints(-1);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("basisPoints");
    }

    [Fact]
    public void FromBasisPoints_Above100Percent_AllowsValue()
    {
        var percentage = Percentage.FromBasisPoints(15000);

        percentage.AsPercent.Should().Be(150m);
    }

    [Fact]
    public void Addition_SumsFractions()
    {
        var a = Percentage.FromPercent(5m);
        var b = Percentage.FromPercent(3m);

        var result = a + b;

        result.Fraction.Should().Be(0.08m);
    }

    [Fact]
    public void Subtraction_SubtractsFractions()
    {
        var a = Percentage.FromPercent(10m);
        var b = Percentage.FromPercent(3m);

        var result = a - b;

        result.Fraction.Should().Be(0.07m);
    }

    [Fact]
    public void Multiplication_ScalesFraction()
    {
        var percentage = Percentage.FromPercent(10m);

        var result = percentage * 2m;

        result.Fraction.Should().Be(0.2m);
    }

    [Fact]
    public void Division_DividesFraction()
    {
        var percentage = Percentage.FromPercent(10m);

        var result = percentage / 2m;

        result.Fraction.Should().Be(0.05m);
    }

    [Fact]
    public void Equality_SameFraction_AreEqual()
    {
        var a = Percentage.FromPercent(5m);
        var b = Percentage.FromBasisPoints(500);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var percentage = Percentage.FromPercent(5.1234m);

        percentage.ToString().Should().Be("5.1234%");
    }

    [Fact]
    public void AsBasisPoints_RoundsToEven()
    {
        var percentage = Percentage.FromFraction(0.00005m);

        percentage.AsBasisPoints.Should().Be(0L);
    }

   [Fact]
    public void Percentage_Equality_SameFraction_True()
    {
        var a = Percentage.FromPercent(5m);
        var b = Percentage.FromPercent(5m);

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Percentage_Equality_DifferentFraction_False()
    {
        var a = Percentage.FromPercent(5m);
        var b = Percentage.FromPercent(10m);

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Percentage_Equality_Null_False()
    {
        var percentage = Percentage.FromPercent(5m);

        percentage.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Percentage_DivideByZero_ThrowsDivideByZeroException()
    {
        var percentage = Percentage.FromPercent(5m);

        Action divide = () => { var _ = percentage / 0m; };
        divide.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void Equality_Null_LeftSide_ReturnsFalse()
    {
        Percentage? a = null;
        Percentage b = Percentage.FromPercent(5m);

        (a == b).Should().BeFalse();
        (b == a).Should().BeFalse();
    }

    [Fact]
    public void Inequality_BothNull_ReturnsTrue()
    {
        Percentage? a = null;
        Percentage? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void FromFraction_Negative_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => Percentage.FromFraction(-0.1m);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Division_ByZero_ThrowsDivideByZeroException()
    {
        var percentage = Percentage.FromPercent(10m);

        Action action = () => { var _ = percentage / 0m; };

        action.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void ToString_ContainsPercentSign()
    {
        var percentage = Percentage.FromPercent(5.1234m);

        string str = percentage.ToString();

        str.Should().EndWith("%");
    }

    [Fact]
    public void FromPercent_Negative_MessageContainsParameterName()
    {
        Action action = () => Percentage.FromPercent(-1m);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("percent");
    }

    [Fact]
    public void FromBasisPoints_Negative_MessageContainsParameterName()
    {
        Action action = () => Percentage.FromBasisPoints(-1);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("basisPoints");
    }

    [Fact]
    public void FromFraction_Negative_MessageContainsParameterName()
    {
        Action action = () => Percentage.FromFraction(-0.1m);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("fraction");
    }

    [Fact]
    public void FromPercent_Negative_MessageContainsExactText()
    {
        Action action = () => Percentage.FromPercent(-1m);

        action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Percent must be non-negative*");
    }

    [Fact]
    public void FromBasisPoints_Negative_MessageContainsExactText()
    {
        Action action = () => Percentage.FromBasisPoints(-1);

        action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Basis points must be non-negative*");
    }

    [Fact]
    public void Division_ByZero_MessageContainsExactText()
    {
        var percentage = Percentage.FromPercent(5m);

        Action action = () => { var _ = percentage / 0m; };

        action.Should().Throw<DivideByZeroException>().WithMessage("*Cannot divide percentage by zero*");
    }

    [Fact]
    public void FromFraction_Negative_MessageContainsExactText()
    {
        Action action = () => Percentage.FromFraction(-0.1m);

        action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Fraction must be non-negative*");
    }
}
