using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a servicing loan is created at confirmed disbursement.
/// </summary>
/// <remarks>
/// Per owner decision E-1 (2026-09-10), the servicing Loan is created only at
/// confirmed disbursement. This event captures the exact approved and accepted
/// terms version used for creation.
/// </remarks>
public sealed class LoanCreated : LoanEvent
{
    public LoanCreated(
        LoanId loanId,
        Guid loanApplicationId,
        int termsVersion,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        ValueObjects.Money principal,
        InterestRate rate,
        LoanTerm term,
        LocalDate disbursementDate,
        LocalDate firstPaymentDate)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        LoanApplicationId = loanApplicationId;
        TermsVersion = termsVersion;
        Principal = principal;
        Rate = rate;
        Term = term;
        DisbursementDate = disbursementDate;
        FirstPaymentDate = firstPaymentDate;
    }

    /// <summary>The originating loan application ID (idempotency key for handoff).</summary>
    public Guid LoanApplicationId { get; }

    /// <summary>The approved and accepted terms version (immutable per E-2).</summary>
    public int TermsVersion { get; }

    public ValueObjects.Money Principal { get; }
    public InterestRate Rate { get; }
    public LoanTerm Term { get; }
    public LocalDate DisbursementDate { get; }
    public LocalDate FirstPaymentDate { get; }
}
