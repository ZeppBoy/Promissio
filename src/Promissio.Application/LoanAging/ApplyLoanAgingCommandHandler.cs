using MediatR;
using Promissio.Application.LoanCreation;

namespace Promissio.Application.LoanAging;

/// <summary>
/// Loads, ages and conditionally appends to the authoritative loan stream.
/// </summary>
public sealed class ApplyLoanAgingCommandHandler
    : IRequestHandler<ApplyLoanAgingCommand, ApplyLoanAgingResult>
{
    private readonly ILoanRepository _repository;

    /// <summary>Creates a handler backed by the authoritative loan repository.</summary>
    /// <param name="repository">The loan persistence port.</param>
    public ApplyLoanAgingCommandHandler(ILoanRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository, nameof(repository));
        _repository = repository;
    }

    /// <summary>Applies the approved aging transition and persists it with optimistic concurrency.</summary>
    /// <param name="request">The aging request.</param>
    /// <param name="cancellationToken">Token used to cancel the workflow.</param>
    /// <returns>An applied, no-change, not-found, or concurrency-conflict result.</returns>
    public async Task<ApplyLoanAgingResult> Handle(
        ApplyLoanAgingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.LoanId, nameof(request.LoanId));

        PersistedLoan? persistedLoan = await _repository
            .LoadAsync(request.LoanId, cancellationToken)
            .ConfigureAwait(false);
        if (persistedLoan is null)
        {
            return new ApplyLoanAgingResult(
                request.LoanId,
                ApplyLoanAgingStatus.NotFound,
                null);
        }

        persistedLoan.Loan.ApplyAging(
            request.DaysPastDue,
            request.EffectiveDate,
            request.RecordedAt,
            request.CorrelationId,
            request.PastDueThreshold,
            request.DefaultThreshold);

        if (persistedLoan.Loan.UncommittedEvents.Count == 0)
        {
            return new ApplyLoanAgingResult(
                request.LoanId,
                ApplyLoanAgingStatus.NoChange,
                persistedLoan.Loan.State);
        }

        LoanSaveStatus saveStatus = await _repository
            .SaveAsync(persistedLoan, cancellationToken)
            .ConfigureAwait(false);
        if (saveStatus == LoanSaveStatus.ConcurrencyConflict)
        {
            return new ApplyLoanAgingResult(
                request.LoanId,
                ApplyLoanAgingStatus.ConcurrencyConflict,
                null);
        }

        return new ApplyLoanAgingResult(
            request.LoanId,
            ApplyLoanAgingStatus.Applied,
            persistedLoan.Loan.State);
    }
}
