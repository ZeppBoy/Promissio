using Marten;
using Promissio.Application.LoanQueries;
using Promissio.Domain.Loan;

namespace Promissio.Infrastructure.LoanQueries;

/// <summary>Reads committed loan summary documents from PostgreSQL.</summary>
public sealed class MartenLoanSummaryReader : ILoanSummaryReader
{
    private readonly IDocumentStore _store;

    /// <summary>Creates a reader backed by the configured document store.</summary>
    /// <param name="store">The Marten document store.</param>
    public MartenLoanSummaryReader(IDocumentStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public async Task<LoanSummary?> ReadAsync(LoanId loanId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loanId);
        await using IQuerySession session = _store.QuerySession();
        return await session.LoadAsync<LoanSummary>(loanId.Value, cancellationToken).ConfigureAwait(false);
    }
}
