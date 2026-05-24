using FsCheck;
using FsCheck.Xunit;
using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Tests.ValueObjects;

public class LoanTermPropertyTests
{
    [Property]
    public bool TotalMonths_ConsistentWithYearsAndMonths(LoanTermMonths months)
    {
        var term = LoanTerm.FromMonths(months.Value);

        return term.TotalMonths == (term.Years * 12 + term.Months);
    }

    [Property]
    public bool FromMonths_RoundTrip(LoanTermMonths months)
    {
        var term = LoanTerm.FromMonths(months.Value);

        return term.TotalMonths == months.Value;
    }

    [Property]
    public bool FromYears_CorrectConversion(LoanTermYears years)
    {
        var term = LoanTerm.FromYears(years.Value);

        return term.TotalMonths == years.Value * 12;
    }

    [Property]
    public bool Equality_BasedOnTotalMonths(LoanTermMonths a, LoanTermMonths b)
    {
        var termA = LoanTerm.FromMonths(a.Value);
        var termB = LoanTerm.FromMonths(b.Value);

        if (a.Value == b.Value)
            return termA == termB;

        return true;
    }

    [Property]
    public bool GetHashCode_ConsistentWithEquality(LoanTermMonths a, LoanTermMonths b)
    {
        var termA = LoanTerm.FromMonths(a.Value);
        var termB = LoanTerm.FromMonths(b.Value);

        if (a.Value == b.Value)
            return termA.GetHashCode() == termB.GetHashCode();

        return true;
    }

    [Property]
    public bool EndDate_IncreasesWithStartDate(LoanTermMonths months)
    {
        var term = LoanTerm.FromMonths(months.Value);
        var date1 = new LocalDate(2024, 1, 1);
        var date2 = new LocalDate(2024, 2, 1);

        var end1 = term.EndDate(date1);
        var end2 = term.EndDate(date2);

        return end2 > end1;
    }

    [Property]
    public bool ToString_ContainsTotalMonths(LoanTermMonths months)
    {
        var term = LoanTerm.FromMonths(months.Value);

        return term.ToString().Contains($"{months.Value} months total");
    }
}

public readonly struct LoanTermMonths
{
    public int Value { get; }

    public LoanTermMonths(int value)
    {
        Value = Math.Max(1, value);
    }
}

public readonly struct LoanTermYears
{
    public int Value { get; }

    public LoanTermYears(int value)
    {
        Value = Math.Max(1, value);
    }
}
