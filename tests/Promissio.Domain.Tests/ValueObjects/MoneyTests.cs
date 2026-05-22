using FluentAssertions;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var money = new Money(100.50m, "USD");

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_RoundsAmountToTwoDecimalPlaces()
    {
        var money = new Money(100.555m, "USD");

        money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentNullException()
    {
        Action action = () => new Money(100m, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Zero_ReturnsZeroMoney()
    {
        var money = Money.Zero("EUR");

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Addition_SameCurrency_ReturnsSum()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a + b;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "EUR");

        Action action = () => { var _ = a + b; };

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtraction_SameCurrency_ReturnsDifference()
    {
        var a = new Money(100m, "USD");
        var b = new Money(30m, "USD");

        var result = a - b;

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Multiplication_ScalesAmount()
    {
        var money = new Money(100m, "USD");

        var result = money * 2.5m;

        result.Amount.Should().Be(250m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Division_DividesAmount()
    {
        var money = new Money(100m, "USD");

        var result = money / 4m;

        result.Amount.Should().Be(25m);
    }

    [Fact]
    public void Division_ByZero_ThrowsDivideByZeroException()
    {
        var money = new Money(100m, "USD");

        Action action = () => { var _ = money / 0m; };

        action.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void Comparison_Operators_WorkCorrectly()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        (a > b).Should().BeTrue();
        (a < b).Should().BeFalse();
        (a >= b).Should().BeTrue();
        (a <= b).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "EUR");

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquality()
    {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var money = new Money(123.45m, "EUR");

        money.ToString().Should().Be("123.45 EUR");
    }

    [Fact]
    public void Multiplication_ReversedOrder_WorksCorrectly()
    {
        var money = new Money(100m, "USD");

        var result = 2.5m * money;

        result.Amount.Should().Be(250m);
    }

    [Fact]
    public void Addition_Associative_ForSameCurrency()
    {
        var a = new Money(10m, "USD");
        var b = new Money(20m, "USD");
        var c = new Money(30m, "USD");

        var leftAssoc = (a + b) + c;
        var rightAssoc = a + (b + c);

        leftAssoc.Should().Be(rightAssoc);
    }

    [Fact]
    public void Addition_Commutative_ForSameCurrency()
    {
        var a = new Money(10m, "USD");
        var b = new Money(20m, "USD");

        (a + b).Should().Be(b + a);
    }

    [Fact]
    public void Addition_ZeroIsIdentity()
    {
        var money = new Money(100m, "USD");
        var zero = Money.Zero("USD");

        (money + zero).Should().Be(money);
    }
}
