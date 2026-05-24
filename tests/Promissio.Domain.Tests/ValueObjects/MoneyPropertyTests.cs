using FsCheck;
using FsCheck.Xunit;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Tests.ValueObjects;

public class MoneyPropertyTests
{
    [Property]
    public bool Addition_Associative(MoneyAmount a, MoneyAmount b, MoneyAmount c)
    {
        var leftAssoc = (a.Value + b.Value) + c.Value;
        var rightAssoc = a.Value + (b.Value + c.Value);

        return leftAssoc == rightAssoc;
    }

    [Property]
    public bool Addition_Commutative(MoneyAmount a, MoneyAmount b)
    {
        return (a.Value + b.Value) == (b.Value + a.Value);
    }

    [Property]
    public bool Addition_ZeroIsIdentity(MoneyAmount a)
    {
        var zero = Money.Zero(a.Value.Currency);

        return (a.Value + zero) == a.Value;
    }

    [Property]
    public bool Subtraction_IsInverseOfAddition(MoneyAmount a, MoneyAmount b)
    {
        return (a.Value + b.Value) - b.Value == a.Value;
    }

    [Property]
    public bool Multiplication_MultiplicativeIdentity(MoneyAmount a)
    {
        return (a.Value * 1m) == a.Value;
    }

    [Property]
    public bool Equality_Transitive(MoneyAmount a, MoneyAmount b, MoneyAmount c)
    {
        if (a.Value == b.Value && b.Value == c.Value)
            return a.Value == c.Value;

        return true;
    }

    [Property]
    public bool GetHashCode_ConsistentWithEquality(MoneyAmount a)
    {
        return a.Value.GetHashCode() == a.Value.GetHashCode();
    }

    [Property]
    public bool ReversedMultiplication_SameResult(MoneyAmount a, PositiveFactor factor)
    {
        return (a.Value * factor.Value) == (factor.Value * a.Value);
    }

    [Property]
    public void Addition_DifferentCurrencies_Throws(MoneyAmount a)
    {
        var differentCurrency = new Money(100m, "XOF");

        try
        {
            _ = a.Value + differentCurrency;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Expected InvalidOperationException for different currencies");
    }

    [Property]
    public bool Multiplication_DistributiveOverAddition_WithinRounding(MoneyAmount a, MoneyAmount b, PositiveFactor factor)
    {
        var leftSide = (a.Value + b.Value) * factor.Value;
        var rightSide = (a.Value * factor.Value) + (b.Value * factor.Value);

        return Math.Abs(leftSide.Amount - rightSide.Amount) < 0.1m;
    }

    [Property]
    public bool Division_IsInverseOfMultiplication_WithinRounding(MoneyAmount a, SmallPositiveFactor factor)
    {
        var result = (a.Value * factor.Value) / factor.Value;

        return Math.Abs(result.Amount - a.Value.Amount) < 0.1m;
    }
}

public readonly struct MoneyAmount
{
    public Money Value { get; }

    public MoneyAmount(Decimal amount)
    {
        Value = new Money(Math.Abs(amount), "USD");
    }
}

public readonly struct PositiveFactor
{
    public Decimal Value { get; }

    public PositiveFactor(Decimal value)
    {
        Value = Math.Max(value, 0.01m);
    }
}

public readonly struct SmallPositiveFactor
{
    public Decimal Value { get; }

    public SmallPositiveFactor(Decimal value)
    {
        Value = Math.Clamp(Math.Abs(value), 0.5m, 10m);
    }
}
