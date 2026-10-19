using Marten;
using Promissio.Application.LoanStateQueries;
using Promissio.Domain.Loan;

namespace Promissio.Infrastructure.LoanPersistence;

/// <summary>Reads the inline Marten loan-state projection.</summary>
public sealed class MartenLoanStateReader : ILoanStateReader
{
    private readonly IDocumentStore _store;

    /// <summary>Creates a reader backed by the configured Marten store.</summary>
    /// <param name="store">The Marten document store.</param>
    public MartenLoanStateReader(IDocumentStore store)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));
        _store = store;
    }

    /// <inheritdoc />
    public async Task<LoanStateView?> GetAsync(LoanId loanId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loanId, nameof(loanId));
        await using IQuerySession session = _store.QuerySession();
        LoanStateDocument? document = await session
            .LoadAsync<LoanStateDocument>(loanId.Value, cancellationToken)
            .ConfigureAwait(false);
        return document is null
            ? null
            : new LoanStateView(loanId, document.State, document.DaysPastDue);
    }
}
