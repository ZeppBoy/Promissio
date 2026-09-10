using NodaTime;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Loan;

/// <summary>
/// Aggregate root for a servicing loan.
/// </summary>
/// <remarks>
/// The Loan is created at confirmed disbursement (owner decision E-1, 2026-09-10)
/// using the exact approved and accepted terms version. Terms are immutable after
/// creation (E-2). State transitions follow the table in
/// <c>docs/domain/loan-state-machine.md</c>.
///
/// Failure boundary (E-5): commands rejected by a business validation rule return
/// <see cref="Result{T}"/>; transitions prohibited by the state-transition table
/// throw <see cref="InvalidStateTransitionException"/>. Both leave state unchanged
/// and emit no events.
/// </remarks>
public sealed class Loan
{
    private readonly List<LoanEvent> _uncommittedEvents = [];

    /// <summary>The identity of this loan.</summary>
    public LoanId Id { get; }

    /// <summary>The current state of the loan lifecycle.</summary>
    public LoanState State { get; private set; }

    /// <summary>The original principal amount at disbursement.</summary>
    public Money Principal { get; }

    /// <summary>The current remaining balance.</summary>
    public Money RemainingBalance { get; private set; }

    /// <summary>The interest rate for this loan (immutable after creation).</summary>
    public InterestRate Rate { get; }

    /// <summary>The loan term in months (immutable after creation).</summary>
    public LoanTerm Term { get; }

    /// <summary>The disbursement date.</summary>
    public LocalDate DisbursementDate { get; }

    /// <summary>The date of the first scheduled payment.</summary>
    public LocalDate FirstPaymentDate { get; }

    /// <summary>The date the loan was created (at disbursement).</summary>
    public LocalDate CreationDate { get; }

    /// <summary>Days past due, if the loan is in a delinquent state.</summary>
    public int? DaysPastDue { get; private set; }

    /// <summary>
    /// Gets the list of uncommitted domain events emitted by commands since the last commit.
    /// </summary>
    public IReadOnlyList<LoanEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Creates a new loan at confirmed disbursement.
    /// </summary>
    /// <param name="id">The loan identity.</param>
    /// <param name="loanApplicationId">The originating loan application (idempotency key).</param>
    /// <param name="principal">The loan principal.</param>
    /// <param name="rate">The interest rate.</param>
    /// <param name="term">The loan term.</param>
    /// <param name="disbursementDate">The disbursement date.</param>
    /// <param name="firstPaymentDate">The first payment date.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier for the creation workflow.</param>
    /// <exception cref="ArgumentOutOfRangeException">If principal is not positive.</exception>
    public Loan(
        LoanId id,
        Guid loanApplicationId,
        Money principal,
        InterestRate rate,
        LoanTerm term,
        LocalDate disbursementDate,
        LocalDate firstPaymentDate,
        Instant recordedAt,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        ArgumentNullException.ThrowIfNull(principal, nameof(principal));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        ArgumentNullException.ThrowIfNull(term, nameof(term));

        if (principal.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(principal), "Principal must be positive.");

        if (firstPaymentDate < disbursementDate)
            throw new ArgumentOutOfRangeException(nameof(firstPaymentDate),
                "First payment date must be on or after the disbursement date.");

        Id = id;
        Principal = principal;
        RemainingBalance = principal;
        Rate = rate;
        Term = term;
        DisbursementDate = disbursementDate;
        FirstPaymentDate = firstPaymentDate;
        CreationDate = disbursementDate;
        State = LoanState.Disbursed;
        LoanApplicationId = loanApplicationId;

        _uncommittedEvents.Add(new LoanCreated(
            id,
            disbursementDate,
            recordedAt,
            correlationId,
            principal,
            rate,
            term,
            disbursementDate,
            firstPaymentDate));
    }

    /// <summary>The originating loan application ID (idempotency key for handoff).</summary>
    public Guid LoanApplicationId { get; }

    /// <summary>
    /// Transitions the loan from Disbursed to Active on the first business day after disbursement.
    /// </summary>
    /// <param name="effectiveDate">The business-effective date of activation.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier.</param>
    /// <exception cref="InvalidStateTransitionException">If the loan is not in the Disbursed state.</exception>
    public void Activate(LocalDate effectiveDate, Instant recordedAt, Guid correlationId)
    {
        if (State != LoanState.Disbursed)
            throw new InvalidStateTransitionException(State, "Activate",
                $"Loan can only be activated from Disbursed state, not {State}.");

        State = LoanState.Active;
        _uncommittedEvents.Add(new LoanActivated(
            Id,
            effectiveDate,
            recordedAt,
            correlationId,
            DisbursementDate));
    }

    /// <summary>
    /// Records a payment received against the loan.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="effectiveDate">The business-effective date of the payment.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier.</param>
    /// <returns><see cref="Result{T}"/> containing the new balance on success, or an error message on failure.</returns>
    /// <exception cref="InvalidStateTransitionException">If the loan is in a terminal state (WrittenOff, Restructured, Recovered).</exception>
    /// <remarks>
    /// Business validation (E-5): a payment of zero or negative amount returns
    /// <see cref="Result{T}"/> with an error. A payment to a loan in a terminal
    /// state throws <see cref="InvalidStateTransitionException"/>.
    /// </remarks>
    public Result<Money> RecordPayment(Money amount, LocalDate effectiveDate, Instant recordedAt, Guid correlationId)
    {
        if (amount.Amount <= 0)
            return Result<Money>.Failure("Payment amount must be positive.");

        if (State is LoanState.WrittenOff or LoanState.Restructured or LoanState.Recovered)
            throw new InvalidStateTransitionException(State, "RecordPayment",
                $"Cannot record a payment on a loan in {State} state.");

        if (amount.Amount > RemainingBalance.Amount)
            return Result<Money>.Failure(
                $"Payment amount ({amount}) exceeds remaining balance ({RemainingBalance}).");

        Money newBalance = RemainingBalance - amount;
        RemainingBalance = newBalance;

        // State transition back to Active is handled by the aging process, not by payment.
        // The payment event itself does not change state.
        var paymentEvent = new PaymentReceived(
            Id,
            effectiveDate,
            recordedAt,
            correlationId,
            amount,
            newBalance);

        _uncommittedEvents.Add(paymentEvent);
        return Result<Money>.Success(newBalance);
    }

    /// <summary>
    /// Applies aging: transitions the loan to the appropriate delinquent state based on days past due.
    /// </summary>
    /// <param name="daysPastDue">The number of days the loan is past due.</param>
    /// <param name="effectiveDate">The business-effective date.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier.</param>
    /// <param name="pastDueThreshold">Days past due threshold for PastDue state (default 1).</param>
    /// <param name="defaultThreshold">Days past due threshold for Defaulted state (default 90).</param>
    /// <exception cref="InvalidStateTransitionException">If the loan is in a state from which aging cannot transition.</exception>
    /// <remarks>
    /// Per AGENTS.md §8:
    /// - Active → InGrace → PastDue based on days past due threshold (default 1).
    /// - PastDue → Defaulted based on days past due threshold (default 90).
    /// </remarks>
    public void ApplyAging(
        int daysPastDue,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        int pastDueThreshold = 1,
        int defaultThreshold = 90)
    {
        if (daysPastDue < 0)
            throw new ArgumentOutOfRangeException(nameof(daysPastDue), "Days past due cannot be negative.");

        // Terminal states cannot age.
        if (State is LoanState.WrittenOff or LoanState.Restructured or LoanState.Recovered)
            throw new InvalidStateTransitionException(State, "ApplyAging",
                $"Cannot apply aging to a loan in {State} state.");

        if (daysPastDue == 0)
        {
            // No aging: if the loan was delinquent, it returns to Active.
            if (State is LoanState.PastDue or LoanState.InGrace or LoanState.Defaulted)
            {
                State = LoanState.Active;
                DaysPastDue = null;
                _uncommittedEvents.Add(new LoanBecameActive(
                    Id, effectiveDate, recordedAt, correlationId, DaysPastDue ?? 0));
            }
            return;
        }

        if (daysPastDue >= defaultThreshold)
        {
            if (State is LoanState.Disbursed)
                throw new InvalidStateTransitionException(State, "ApplyAging",
                    $"Cannot age a loan that is still Disbursed.");

            State = LoanState.Defaulted;
            DaysPastDue = daysPastDue;
            _uncommittedEvents.Add(new LoanDefaulted(
                Id, effectiveDate, recordedAt, correlationId, daysPastDue));
        }
        else if (daysPastDue >= pastDueThreshold)
        {
            if (State is LoanState.Disbursed)
                throw new InvalidStateTransitionException(State, "ApplyAging",
                    $"Cannot age a loan that is still Disbursed.");

            State = LoanState.PastDue;
            DaysPastDue = daysPastDue;
            _uncommittedEvents.Add(new LoanBecamePastDue(
                Id, effectiveDate, recordedAt, correlationId, daysPastDue));
        }
        else
        {
            // daysPastDue is between 0 and pastDueThreshold: in grace period
            State = LoanState.InGrace;
            DaysPastDue = daysPastDue;
            _uncommittedEvents.Add(new LoanEnteredGracePeriod(
                Id, effectiveDate, recordedAt, correlationId, daysPastDue));
        }
    }

    /// <summary>
    /// Writes off the loan.
    /// </summary>
    /// <param name="reason">The reason for write-off.</param>
    /// <param name="effectiveDate">The business-effective date.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier.</param>
    /// <exception cref="InvalidStateTransitionException">If the loan is not in the Defaulted state.</exception>
    public void WriteOff(string reason, LocalDate effectiveDate, Instant recordedAt, Guid correlationId)
    {
        if (State != LoanState.Defaulted)
            throw new InvalidStateTransitionException(State, "WriteOff",
                $"Loan can only be written off from Defaulted state, not {State}.");

        ArgumentNullException.ThrowIfNull(reason, nameof(reason));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Write-off reason must not be empty.", nameof(reason));

        State = LoanState.WrittenOff;
        _uncommittedEvents.Add(new LoanWrittenOff(
            Id, effectiveDate, recordedAt, correlationId, reason));
    }

    /// <summary>
    /// Restructures the loan.
    /// </summary>
    /// <param name="restructuringPlan">Description of the restructuring plan.</param>
    /// <param name="effectiveDate">The business-effective date.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier.</param>
    /// <exception cref="InvalidStateTransitionException">If the loan is not in the Defaulted state.</exception>
    public void Restructure(string restructuringPlan, LocalDate effectiveDate, Instant recordedAt, Guid correlationId)
    {
        if (State != LoanState.Defaulted)
            throw new InvalidStateTransitionException(State, "Restructure",
                $"Loan can only be restructured from Defaulted state, not {State}.");

        ArgumentNullException.ThrowIfNull(restructuringPlan, nameof(restructuringPlan));
        if (string.IsNullOrWhiteSpace(restructuringPlan))
            throw new ArgumentException("Restructuring plan must not be empty.", nameof(restructuringPlan));

        State = LoanState.Restructured;
        _uncommittedEvents.Add(new LoanRestructured(
            Id, effectiveDate, recordedAt, correlationId, restructuringPlan));
    }

    /// <summary>
    /// Recovers the loan.
    /// </summary>
    /// <param name="recoveryMethod">Description of the recovery method.</param>
    /// <param name="effectiveDate">The business-effective date.</param>
    /// <param name="recordedAt">The recorded time.</param>
    /// <param name="correlationId">Correlation identifier.</param>
    /// <exception cref="InvalidStateTransitionException">If the loan is not in the Defaulted state.</exception>
    public void Recover(string recoveryMethod, LocalDate effectiveDate, Instant recordedAt, Guid correlationId)
    {
        if (State != LoanState.Defaulted)
            throw new InvalidStateTransitionException(State, "Recover",
                $"Loan can only be recovered from Defaulted state, not {State}.");

        ArgumentNullException.ThrowIfNull(recoveryMethod, nameof(recoveryMethod));
        if (string.IsNullOrWhiteSpace(recoveryMethod))
            throw new ArgumentException("Recovery method must not be empty.", nameof(recoveryMethod));

        State = LoanState.Recovered;
        _uncommittedEvents.Add(new LoanRecovered(
            Id, effectiveDate, recordedAt, correlationId, recoveryMethod));
    }

    /// <summary>
    /// Clears the uncommitted events list after they have been persisted.
    /// </summary>
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    public override string ToString() =>
        $"Loan({Id}, State={State}, Principal={Principal}, Balance={RemainingBalance}, " +
        $"Rate={Rate}, Term={Term}, Disbursed={DisbursementDate:yyyy-MM-dd})";
}
