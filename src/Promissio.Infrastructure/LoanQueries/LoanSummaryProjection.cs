using JasperFx.Events;
using Marten.Events.Aggregation;
using Promissio.Application.LoanQueries;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;

namespace Promissio.Infrastructure.LoanQueries;

/// <summary>Derives the current loan summary from committed events in stream order.</summary>
/// <remarks>Runs inline in the append transaction; see ADR-0009 for rebuild and compatibility policy.</remarks>
public sealed class LoanSummaryProjection : SingleStreamProjection<LoanSummary, Guid>
{
    /// <summary>Configures the loan event types consumed during writes and rebuilds.</summary>
    public LoanSummaryProjection()
    {
        IncludeType<LoanCreated>();
        IncludeType<LoanActivated>();
        IncludeType<LoanEnteredGracePeriod>();
        IncludeType<LoanBecamePastDue>();
        IncludeType<LoanBecameActive>();
        IncludeType<LoanDefaulted>();
        IncludeType<LoanWrittenOff>();
        IncludeType<LoanRestructured>();
        IncludeType<LoanRecovered>();
        IncludeType<PaymentReceived>();
    }

    /// <inheritdoc />
    public override LoanSummary Evolve(LoanSummary? snapshot, Guid id, IEvent @event)
    {
        if (@event.Data is LoanCreated created)
        {
            return new LoanSummary(id, created.LoanApplicationId, created.TermsVersion,
                created.Principal, created.DisbursementDate, created.FirstPaymentDate,
                LoanState.Disbursed, null, @event.Version);
        }

        if (snapshot is null)
            throw new InvalidOperationException("A loan summary requires LoanCreated as its first event.");

        LoanSummary updated = @event.Data switch
        {
            LoanActivated or LoanBecameActive => snapshot with { State = LoanState.Active, DaysPastDue = null },
            LoanEnteredGracePeriod grace => snapshot with { State = LoanState.InGrace, DaysPastDue = grace.DaysPastDue },
            LoanBecamePastDue pastDue => snapshot with { State = LoanState.PastDue, DaysPastDue = pastDue.DaysPastDue },
            LoanDefaulted defaulted => snapshot with { State = LoanState.Defaulted, DaysPastDue = defaulted.DaysPastDue },
            LoanWrittenOff => snapshot with { State = LoanState.WrittenOff },
            LoanRestructured => snapshot with { State = LoanState.Restructured },
            LoanRecovered => snapshot with { State = LoanState.Recovered },
            PaymentReceived => snapshot,
            _ => throw new InvalidOperationException($"Unsupported loan summary event '{@event.Data.GetType().FullName}'.")
        };
        return updated with { StreamVersion = @event.Version };
    }
}
