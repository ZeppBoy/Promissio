using NodaTime;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a defaulted loan is restructured.
/// </summary>
/// <remarks>
/// Per AGENTS.md §8: "Defaulted → WrittenOff / Restructured / Recovered."
/// Restructuring is a terminal state from the default perspective; the loan
/// exits the default workflow.
/// </remarks>
public sealed class LoanRestructured : LoanEvent
{
    public LoanRestructured(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        string restructuringPlan)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        RestructuringPlan = restructuringPlan;
    }

    public string RestructuringPlan { get; }
}
