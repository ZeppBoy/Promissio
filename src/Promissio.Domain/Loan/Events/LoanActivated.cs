using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a loan transitions from <see cref="LoanState.Disbursed"/> to <see cref="LoanState.Active"/>.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "Disbursed → Active on first business day after disbursement."
/// </remarks>
public sealed class LoanActivated : LoanEvent
{
    public LoanActivated(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        LocalDate disbursementDate)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        DisbursementDate = disbursementDate;
    }

    public LocalDate DisbursementDate { get; }
}
