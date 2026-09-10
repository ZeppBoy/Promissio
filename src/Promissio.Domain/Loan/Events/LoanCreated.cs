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
        Principal = principal;
        Rate = rate;
        Term = term;
        DisbursementDate = disbursementDate;
        FirstPaymentDate = firstPaymentDate;
    }

    public ValueObjects.Money Principal { get; }
    public InterestRate Rate { get; }
    public LoanTerm Term { get; }
    public LocalDate DisbursementDate { get; }
    public LocalDate FirstPaymentDate { get; }
}
