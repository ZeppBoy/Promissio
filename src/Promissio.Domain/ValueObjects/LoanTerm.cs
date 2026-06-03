using NodaTime;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Immutable representation of a loan term using NodaTime Period.
/// </summary>
/// <remarks>
/// LoanTerm represents the duration of a loan contract, typically expressed in months.
/// Uses NodaTime Period for precise calendar arithmetic.
/// Equality is based on TotalMonths only; Years and Months are derived properties.
/// </remarks>
public sealed record LoanTerm
{
    /// <summary>
    /// Total months in the term. For terms with years, this includes converted years.
    /// </summary>
    public int TotalMonths { get; }

    /// <summary>
    /// Years component of the term.
    /// </summary>
    public int Years => TotalMonths / 12;

    /// <summary>
    /// Months component of the term (remaining after years).
    /// </summary>
    public int Months => TotalMonths % 12;

    private LoanTerm(int totalMonths)
    {
        TotalMonths = totalMonths;
    }

    public static LoanTerm FromMonths(int months)
    {
        if (months <= 0)
            throw new ArgumentOutOfRangeException(nameof(months), "Loan term must be positive.");

        return new LoanTerm(months);
    }

    public static LoanTerm FromYears(int years)
    {
        if (years <= 0)
            throw new ArgumentOutOfRangeException(nameof(years), "Loan term must be positive.");

        return new LoanTerm(years * 12);
    }

    public LocalDate EndDate(LocalDate startDate) => startDate.PlusMonths(TotalMonths);

    public override string ToString() => $"LoanTerm({Years}y {Months}m, {TotalMonths} months total)";
}
