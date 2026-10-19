using Promissio.Domain.Loan;

namespace Promissio.Application.LoanStateQueries;

/// <summary>Reads the derived current-state view without loading an event stream.</summary>
public interface ILoanStateReader
{
    /// <summary>Returns a loan's projected state, or null when no view exists.</summary>
    /// <param name="loanId">The loan to find.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    Task<LoanStateView?> GetAsync(LoanId loanId, CancellationToken cancellationToken);
}
