using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a loan becomes past due.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "Active / InGrace → PastDue based on days past due threshold
/// (configurable, default 1)."
/// </remarks>
public sealed class LoanBecamePastDue : LoanEvent
{
    public LoanBecamePastDue(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        int daysPastDue)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        DaysPastDue = daysPastDue;
    }

    public int DaysPastDue { get; }
}
