using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a loan is written off.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "Defaulted → WrittenOff / Restructured / Recovered."
/// Writing off is a terminal state.
/// </remarks>
public sealed class LoanWrittenOff : LoanEvent
{
    public LoanWrittenOff(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        string reason)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
