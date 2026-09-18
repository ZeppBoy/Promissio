using MediatR;
using Promissio.Application.LoanCreation;

namespace Promissio.Application.LoanActivation;

/// <summary>
/// Loads, activates and conditionally appends to the authoritative loan stream.
/// </summary>
public sealed class ActivateLoanCommandHandler : IRequestHandler<ActivateLoanCommand, ActivateLoanResult>
{
    private readonly ILoanRepository _repository;

    /// <summary>Creates a handler backed by the authoritative loan repository.</summary>
    /// <param name="repository">The loan persistence port.</param>
    public ActivateLoanCommandHandler(ILoanRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository, nameof(repository));
        _repository = repository;
    }

    /// <summary>Applies the approved activation transition and persists it with optimistic concurrency.</summary>
    /// <param name="request">The activation request.</param>
    /// <param name="cancellationToken">Token used to cancel the workflow.</param>
    /// <returns>A typed activated, not-found, or concurrency-conflict result.</returns>
    public async Task<ActivateLoanResult> Handle(
        ActivateLoanCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.LoanId, nameof(request.LoanId));
        ArgumentNullException.ThrowIfNull(request.Calendar, nameof(request.Calendar));

        PersistedLoan? persistedLoan = await _repository
            .LoadAsync(request.LoanId, cancellationToken)
            .ConfigureAwait(false);
        if (persistedLoan is null)
            return new ActivateLoanResult(request.LoanId, ActivateLoanStatus.NotFound);

        persistedLoan.Loan.Activate(
            request.EffectiveDate,
            request.Calendar,
            request.RecordedAt,
            request.CorrelationId);

        LoanSaveStatus saveStatus = await _repository
            .SaveAsync(persistedLoan, cancellationToken)
            .ConfigureAwait(false);

        ActivateLoanStatus activationStatus = saveStatus == LoanSaveStatus.Saved
            ? ActivateLoanStatus.Activated
            : ActivateLoanStatus.ConcurrencyConflict;
        return new ActivateLoanResult(request.LoanId, activationStatus);
    }
}
