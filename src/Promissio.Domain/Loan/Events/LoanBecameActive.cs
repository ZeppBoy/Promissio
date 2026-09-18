using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a past-due or grace-period loan returns to active performing status.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8 common transitions: when a borrower makes a payment that
/// brings the loan back to performing, the state transitions back to Active.
/// </remarks>
public sealed class LoanBecameActive : LoanEvent
{
    public LoanBecameActive(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        int previousDaysPastDue)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        PreviousDaysPastDue = previousDaysPastDue;
    }

    public int PreviousDaysPastDue { get; }
}
