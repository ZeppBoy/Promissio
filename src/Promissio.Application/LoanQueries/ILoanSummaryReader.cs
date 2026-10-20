using Promissio.Domain.Loan;

namespace Promissio.Application.LoanQueries;

/// <summary>Reads derived loan summaries without replaying authoritative streams.</summary>
public interface ILoanSummaryReader
{
    /// <summary>Returns the committed summary for a loan, or null when no summary exists.</summary>
    /// <param name="loanId">The loan to read.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>The derived summary, or null.</returns>
    Task<LoanSummary?> ReadAsync(LoanId loanId, CancellationToken cancellationToken);
}
