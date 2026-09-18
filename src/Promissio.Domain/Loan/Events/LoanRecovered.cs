using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a defaulted loan is recovered.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "Defaulted → WrittenOff / Restructured / Recovered."
/// Recovery is a terminal state.
/// </remarks>
public sealed class LoanRecovered : LoanEvent
{
    public LoanRecovered(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        string recoveryMethod)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        RecoveryMethod = recoveryMethod;
    }

    public string RecoveryMethod { get; }
}
