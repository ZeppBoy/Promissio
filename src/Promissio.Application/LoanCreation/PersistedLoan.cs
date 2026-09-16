using Promissio.Domain.Loan;

namespace Promissio.Application.LoanCreation;

/// <summary>
/// A loan reconstructed from its authoritative event stream.
/// </summary>
/// <param name="Loan">The reconstructed aggregate.</param>
/// <param name="StreamVersion">The version of the last applied event.</param>
public sealed record PersistedLoan(Loan Loan, long StreamVersion);
