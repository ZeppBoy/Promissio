using Marten.Events.Aggregation;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;

namespace Promissio.Infrastructure.LoanPersistence;

/// <summary>Derives the current lifecycle state from a loan's event stream.</summary>
public sealed partial class LoanStateProjection : SingleStreamProjection<LoanStateDocument, Guid>
{
    /// <summary>Names the projection for targeted operational rebuilds.</summary>
    public LoanStateProjection()
    {
        Name = "LoanState";
    }

    /// <summary>Creates the view when a loan stream begins.</summary>
    public LoanStateDocument Create(LoanCreated created) =>
        new(created.LoanId.Value, LoanState.Disbursed, null);

    /// <summary>Projects initial activation.</summary>
    public LoanStateDocument Apply(LoanActivated _, LoanStateDocument view) =>
        view with { State = LoanState.Active, DaysPastDue = null };

    /// <summary>Projects a return to performing status.</summary>
    public LoanStateDocument Apply(LoanBecameActive _, LoanStateDocument view) =>
        view with { State = LoanState.Active, DaysPastDue = null };

    /// <summary>Projects entry into a grace period.</summary>
    public LoanStateDocument Apply(LoanEnteredGracePeriod entered, LoanStateDocument view) =>
        view with { State = LoanState.InGrace, DaysPastDue = entered.DaysPastDue };

    /// <summary>Projects a past-due loan or a change in its days past due.</summary>
    public LoanStateDocument Apply(LoanBecamePastDue pastDue, LoanStateDocument view) =>
        view with { State = LoanState.PastDue, DaysPastDue = pastDue.DaysPastDue };

    /// <summary>Projects default.</summary>
    public LoanStateDocument Apply(LoanDefaulted defaulted, LoanStateDocument view) =>
        view with { State = LoanState.Defaulted, DaysPastDue = defaulted.DaysPastDue };

    /// <summary>Projects a write-off.</summary>
    public LoanStateDocument Apply(LoanWrittenOff _, LoanStateDocument view) =>
        view with { State = LoanState.WrittenOff };

    /// <summary>Projects restructuring.</summary>
    public LoanStateDocument Apply(LoanRestructured _, LoanStateDocument view) =>
        view with { State = LoanState.Restructured };

    /// <summary>Projects recovery.</summary>
    public LoanStateDocument Apply(LoanRecovered _, LoanStateDocument view) =>
        view with { State = LoanState.Recovered };
}
