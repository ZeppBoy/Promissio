using MediatR;
using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Application.LoanCreation;

/// <summary>
/// Creates a servicing loan from the exact approved terms at confirmed disbursement.
/// </summary>
/// <param name="LoanApplicationId">The originating application and handoff idempotency key.</param>
/// <param name="TermsVersion">The approved and borrower-accepted terms version.</param>
/// <param name="Principal">The disbursed principal.</param>
/// <param name="Rate">The immutable contractual interest rate.</param>
/// <param name="Term">The immutable contractual loan term.</param>
/// <param name="DisbursementDate">The confirmed business date of disbursement.</param>
/// <param name="FirstPaymentDate">The first scheduled payment date.</param>
/// <param name="RecordedAt">The instant at which the handoff was recorded.</param>
/// <param name="CorrelationId">The workflow correlation identifier.</param>
public sealed record CreateLoanCommand(
    Guid LoanApplicationId,
    int TermsVersion,
    Money Principal,
    InterestRate Rate,
    LoanTerm Term,
    LocalDate DisbursementDate,
    LocalDate FirstPaymentDate,
    Instant RecordedAt,
    Guid CorrelationId) : IRequest<LoanCreationResult>;
