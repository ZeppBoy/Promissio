using NodaTime;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Immutable representation of a loan term using NodaTime Period.
/// </summary>
/// <remarks>
/// LoanTerm represents the duration of a loan contract, typically expressed in months.
/// Uses NodaTime Period for precise calendar arithmetic.
/// </remarks>
public sealed class LoanTerm : IEquatable<LoanTerm>
{
    private LoanTerm(int totalMonths)
    {
        TotalMonths = totalMonths;
        Years = totalMonths / 12;
        Months = totalMonths % 12;
    }

    /// <summary>
    /// Total months in the term. For terms with years, this includes converted years.
    /// </summary>
    public int TotalMonths { get; }

    /// <summary>
    /// Years component of the term.
    /// </summary>
    public int Years { get; }

    /// <summary>
    /// Months component of the term (remaining after years).
    /// </summary>
    public int Months { get; }

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

    public LocalDate EndDate(LocalDate startDate)
    {
        return startDate.PlusMonths(TotalMonths);
    }

    #region Equality

    public bool Equals(LoanTerm? other) => other is not null && this.TotalMonths == other.TotalMonths;

    public static bool operator ==(LoanTerm? left, LoanTerm? right) => Equals(left, right);

    public static bool operator !=(LoanTerm? left, LoanTerm? right) => !Equals(left, right);

    public override bool Equals(object? obj) => Equals(obj as LoanTerm);

    public override int GetHashCode() => TotalMonths.GetHashCode();

    public override string ToString() => $"LoanTerm({Years}y {Months}m, {TotalMonths} months total)";

    #endregion
}
