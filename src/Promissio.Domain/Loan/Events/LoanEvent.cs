using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Base type for all loan domain events.
/// </summary>
/// <remarks>
/// Events are immutable. They carry enough data that consumers don't need
/// to query the aggregate (AGENTS.md §6). Each event includes identity, temporal,
/// and causation fields in addition to its domain payload.
/// </remarks>
public abstract class LoanEvent
{
    protected LoanEvent(LoanId loanId, LocalDate effectiveDate, Instant recordedAt, Guid correlationId)
    {
        LoanId = loanId;
        EffectiveDate = effectiveDate;
        RecordedAt = recordedAt;
        CorrelationId = correlationId;
    }

    /// <summary>The identity of the loan this event belongs to.</summary>
    public LoanId LoanId { get; }

    /// <summary>The business-effective date of this event.</summary>
    public LocalDate EffectiveDate { get; }

    /// <summary>The time at which this event was recorded (persisted).</summary>
    public Instant RecordedAt { get; }

    /// <summary>Correlation identifier linking this event to the command that caused it.</summary>
    public Guid CorrelationId { get; }
}
