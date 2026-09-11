using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a loan transitions into the grace period.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "Active / InGrace → PastDue based on days past due threshold."
/// This event fires when the loan first enters the InGrace state.
/// </remarks>
public sealed class LoanEnteredGracePeriod : LoanEvent
{
    public LoanEnteredGracePeriod(
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
