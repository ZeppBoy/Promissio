using JasperFx;
using JasperFx.Events;
using Marten;
using Promissio.Application.LoanCreation;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Infrastructure.LoanPersistence;

/// <summary>
/// Persists loans as authoritative Marten event streams.
/// </summary>
public sealed class MartenLoanRepository : ILoanRepository
{
    private readonly IDocumentStore _store;

    /// <summary>
    /// Creates a repository backed by a configured Marten document store.
    /// </summary>
    /// <param name="store">The configured Marten store.</param>
    public MartenLoanRepository(IDocumentStore store)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));
        _store = store;
    }

    /// <inheritdoc />
    public async Task<LoanCreationResult> CreateAsync(
        LoanRoot loan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loan, nameof(loan));
        if (loan.UncommittedEvents.Count != 1 || loan.UncommittedEvents[0] is not LoanCreated)
        {
            throw new ArgumentException(
                "A new loan stream must contain exactly one uncommitted LoanCreated event.",
                nameof(loan));
        }

        LoanCreationResult? existing = await ResolveExistingAsync(
            loan,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        await using IDocumentSession session = _store.LightweightSession();
        session.Insert(new LoanCreationReceipt(loan.LoanApplicationId, loan.Id.Value));

        object[] events = loan.UncommittedEvents.Cast<object>().ToArray();
        session.Events.StartStream<LoanRoot>(loan.Id.Value, events);

        try
        {
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            loan.ClearUncommittedEvents();
            return new LoanCreationResult(loan.Id, LoanCreationStatus.Created);
        }
        catch (DocumentAlreadyExistsException)
        {
            LoanCreationResult concurrentResult = await ResolveExistingAsync(
                loan,
                cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "The loan handoff idempotency record conflicted but could not be reloaded.");

            return concurrentResult;
        }
    }

    /// <inheritdoc />
    public async Task<PersistedLoan?> LoadAsync(
        LoanId loanId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loanId, nameof(loanId));

        await using IQuerySession session = _store.QuerySession();
        IReadOnlyList<IEvent> storedEvents = await session.Events
            .FetchStreamAsync(loanId.Value, token: cancellationToken)
            .ConfigureAwait(false);

        if (storedEvents.Count == 0)
            return null;

        List<LoanEvent> domainEvents = new(storedEvents.Count);
        foreach (IEvent storedEvent in storedEvents)
        {
            if (storedEvent.Data is not LoanEvent domainEvent)
            {
                throw new InvalidOperationException(
                    $"Loan stream {loanId} contains unsupported event type " +
                    $"'{storedEvent.Data.GetType().FullName}'.");
            }

            domainEvents.Add(domainEvent);
        }

        LoanRoot loan = LoanRoot.Rehydrate(domainEvents);
        return new PersistedLoan(loan, storedEvents[^1].Version);
    }

    /// <inheritdoc />
    public async Task<LoanSaveStatus> SaveAsync(
        PersistedLoan persistedLoan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(persistedLoan, nameof(persistedLoan));
        if (persistedLoan.Loan.UncommittedEvents.Count == 0)
        {
            throw new ArgumentException(
                "An existing loan stream update must contain at least one uncommitted event.",
                nameof(persistedLoan));
        }

        object[] events = persistedLoan.Loan.UncommittedEvents.Cast<object>().ToArray();
        await using IDocumentSession session = _store.LightweightSession();
        // Marten's versioned Append overload accepts the expected maximum version
        // after this batch, not the currently loaded version.
        long expectedVersionAfterAppend = persistedLoan.StreamVersion + events.Length;
        session.Events.Append(persistedLoan.Loan.Id.Value, expectedVersionAfterAppend, events);

        try
        {
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            persistedLoan.Loan.ClearUncommittedEvents();
            return LoanSaveStatus.Saved;
        }
        catch (ConcurrencyException)
        {
            return LoanSaveStatus.ConcurrencyConflict;
        }
    }

    private async Task<LoanCreationResult?> ResolveExistingAsync(
        LoanRoot requestedLoan,
        CancellationToken cancellationToken)
    {
        await using IQuerySession session = _store.QuerySession();
        LoanCreationReceipt? receipt = await session
            .LoadAsync<LoanCreationReceipt>(requestedLoan.LoanApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (receipt is null)
            return null;

        PersistedLoan? persisted = await LoadAsync(
            new LoanId(receipt.LoanId),
            cancellationToken).ConfigureAwait(false);
        if (persisted is null)
        {
            throw new InvalidOperationException(
                $"Loan handoff {receipt.Id} references missing stream {receipt.LoanId}.");
        }

        LoanCreationStatus status = MatchesApprovedTerms(persisted.Loan, requestedLoan)
            ? LoanCreationStatus.Existing
            : LoanCreationStatus.Conflict;

        return new LoanCreationResult(persisted.Loan.Id, status);
    }

    private static bool MatchesApprovedTerms(LoanRoot existing, LoanRoot requested) =>
        existing.LoanApplicationId == requested.LoanApplicationId
        && existing.TermsVersion == requested.TermsVersion
        && existing.Principal == requested.Principal
        && existing.Rate == requested.Rate
        && existing.Term == requested.Term
        && existing.DisbursementDate == requested.DisbursementDate
        && existing.FirstPaymentDate == requested.FirstPaymentDate;
}
