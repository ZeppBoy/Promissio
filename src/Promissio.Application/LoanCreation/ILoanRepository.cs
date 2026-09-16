using Promissio.Domain.Loan;

namespace Promissio.Application.LoanCreation;

/// <summary>
/// Defines persistence operations for authoritative loan event streams.
/// </summary>
public interface ILoanRepository
{
    /// <summary>
    /// Atomically creates a loan stream and records the application handoff outcome.
    /// </summary>
    /// <param name="loan">A new disbursed loan whose creation event has not yet been committed.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A typed created, existing, or conflicting handoff result.</returns>
    Task<LoanCreationResult> CreateAsync(Loan loan, CancellationToken cancellationToken);

    /// <summary>
    /// Loads and reconstructs a loan from its event stream.
    /// </summary>
    /// <param name="loanId">The loan stream identity.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The reconstructed loan and stream version, or <c>null</c> when no stream exists.</returns>
    Task<PersistedLoan?> LoadAsync(LoanId loanId, CancellationToken cancellationToken);
}
