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

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromBasisPoints_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => Percentage.FromBasisPoints(10001);

        action.Should().Throw<ArgumentOutOfRangeException>();
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
}
