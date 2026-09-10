using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a loan enters the defaulted state.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "PastDue → Defaulted based on days past due threshold
/// (configurable, default 90)."
/// </remarks>
public sealed class LoanDefaulted : LoanEvent
{
    public LoanDefaulted(
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
