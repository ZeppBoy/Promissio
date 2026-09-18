using MediatR;
using Promissio.Domain.Loan;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Application.LoanCreation;

/// <summary>
/// Orchestrates the idempotent handoff from an approved application to servicing.
/// </summary>
public sealed class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanCreationResult>
{
    private readonly ILoanRepository _repository;

    /// <summary>
    /// Creates a handler backed by the authoritative loan repository.
    /// </summary>
    /// <param name="repository">The loan persistence port.</param>
    public CreateLoanCommandHandler(ILoanRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository, nameof(repository));
        _repository = repository;
    }

    /// <summary>
    /// Creates the domain aggregate and delegates the atomic handoff to persistence.
    /// </summary>
    /// <param name="request">The approved and accepted disbursement terms.</param>
    /// <param name="cancellationToken">Token used to cancel the workflow.</param>
    /// <returns>A typed idempotency result.</returns>
    public Task<LoanCreationResult> Handle(
        CreateLoanCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        LoanRoot loan = new(
            LoanId.New(),
            request.LoanApplicationId,
            request.TermsVersion,
            request.Principal,
            request.Rate,
            request.Term,
            request.DisbursementDate,
            request.FirstPaymentDate,
            request.RecordedAt,
            request.CorrelationId);

        return _repository.CreateAsync(loan, cancellationToken);
    }
}
