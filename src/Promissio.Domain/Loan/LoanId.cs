namespace Promissio.Domain.Loan;

/// <summary>
/// Strongly-typed identity for a servicing loan.
/// </summary>
/// <remarks>
/// Wraps a <see cref="System.Guid"/> to prevent accidental misuse in APIs
/// that expect a loan identifier. Value-based equality.
/// </remarks>
public sealed record LoanId(Guid Value)
{
    /// <summary>Creates a <see cref="LoanId"/> with a new random Guid.</summary>
    public static LoanId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
