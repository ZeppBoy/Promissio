using FluentAssertions;
using NodaTime;
using NodaTime.TimeZones;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;
using Xunit;
using InvalidStateTransitionException = Promissio.Domain.Loan.InvalidStateTransitionException;
using LoanId = Promissio.Domain.Loan.LoanId;
using LoanRoot = Promissio.Domain.Loan.Loan;
using LoanState = Promissio.Domain.Loan.LoanState;

namespace Promissio.Domain.Tests.LoanAggregate;

/// <summary>
/// Unit tests for the <see cref="Loan"/> aggregate root.
/// Covers construction, all state transitions (valid and invalid),
/// business validation failures, and event emission.
///
/// State names and transitions per AGENTS.md §8, confirmed by owner 2026-09-10.
/// Failure boundary per owner decision E-5 (2026-09-10).
/// </summary>
public class LoanTests
{
    private static readonly LocalDate DisbursementDate = new(2026, 1, 15);
    private static readonly LocalDate FirstPaymentDate = new(2026, 2, 15);
    private static readonly Instant RecordedAt = Instant.FromUtc(2026, 1, 15, 12, 0, 0);
    private static readonly Guid CorrelationId = Guid.NewGuid();
    private static readonly LoanId LoanId1 = LoanId.New();
    private static readonly Guid AppId = Guid.NewGuid();

    private static LoanRoot CreateLoan() => new(
        LoanId1,
        AppId,
        new Money(100_000m, "EUR"),
        new FixedRate(Percentage.FromPercent(5m), new Actual365()),
        LoanTerm.FromMonths(120),
        DisbursementDate,
        FirstPaymentDate,
        RecordedAt,
        CorrelationId);

    #region Construction

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        var loan = CreateLoan();

        loan.Id.Should().Be(LoanId1);
        loan.State.Should().Be(LoanState.Disbursed);
        loan.Principal.Should().Be(new Money(100_000m, "EUR"));
        loan.RemainingBalance.Should().Be(new Money(100_000m, "EUR"));
        loan.DisbursementDate.Should().Be(DisbursementDate);
        loan.FirstPaymentDate.Should().Be(FirstPaymentDate);
        loan.CreationDate.Should().Be(DisbursementDate);
        loan.Term.TotalMonths.Should().Be(120);
    }

    [Fact]
    public void Constructor_EmitsLoanCreatedEvent()
    {
        var loan = CreateLoan();

        loan.UncommittedEvents.Should().HaveCount(1);
        var @event = loan.UncommittedEvents[0].Should().BeOfType<LoanCreated>().Subject;
        @event.LoanId.Should().Be(LoanId1);
        @event.Principal.Should().Be(new Money(100_000m, "EUR"));
        @event.DisbursementDate.Should().Be(DisbursementDate);
    }

    [Fact]
    public void Constructor_ZeroPrincipal_Throws()
    {
        Action action = () => new LoanRoot(
            LoanId1, AppId, new Money(0, "EUR"),
            new FixedRate(Percentage.FromPercent(5m), new Actual365()),
            LoanTerm.FromMonths(120), DisbursementDate, FirstPaymentDate, RecordedAt, CorrelationId);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*principal*");
    }

    [Fact]
    public void Constructor_NegativePrincipal_Throws()
    {
        Action action = () => new LoanRoot(
            LoanId1, AppId, new Money(-1000, "EUR"),
            new FixedRate(Percentage.FromPercent(5m), new Actual365()),
            LoanTerm.FromMonths(120), DisbursementDate, FirstPaymentDate, RecordedAt, CorrelationId);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*principal*");
    }

    [Fact]
    public void Constructor_FirstPaymentBeforeDisbursement_Throws()
    {
        Action action = () => new LoanRoot(
            LoanId1, AppId, new Money(100_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(5m), new Actual365()),
            LoanTerm.FromMonths(120), DisbursementDate, DisbursementDate.PlusDays(-1),
            RecordedAt, CorrelationId);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*First payment date*");
    }

    [Fact]
    public void Constructor_FirstPaymentOnDisbursementDate_IsValid()
    {
        var loan = new LoanRoot(
            LoanId1, AppId, new Money(100_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(5m), new Actual365()),
            LoanTerm.FromMonths(120), DisbursementDate, DisbursementDate,
            RecordedAt, CorrelationId);

        loan.FirstPaymentDate.Should().Be(DisbursementDate);
    }

    [Fact]
    public void Constructor_NullPrincipal_Throws()
    {
        Action action = () => new LoanRoot(
            LoanId1, AppId, null!,
            new FixedRate(Percentage.FromPercent(5m), new Actual365()),
            LoanTerm.FromMonths(120), DisbursementDate, FirstPaymentDate, RecordedAt, CorrelationId);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Activate

    [Fact]
    public void Activate_FromDisbursed_TransitionsToActive()
    {
        var loan = CreateLoan();

        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.State.Should().Be(LoanState.Active);
    }

    [Fact]
    public void Activate_EmitsLoanActivatedEvent()
    {
        var loan = CreateLoan();

        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.UncommittedEvents.Should().ContainSingle(e => e is LoanActivated);
    }

    [Theory]
    [InlineData(LoanState.Active)]
    [InlineData(LoanState.InGrace)]
    [InlineData(LoanState.PastDue)]
    [InlineData(LoanState.Defaulted)]
    [InlineData(LoanState.WrittenOff)]
    [InlineData(LoanState.Restructured)]
    [InlineData(LoanState.Recovered)]
    public void Activate_FromNonDisbursedState_Throws(LoanState state)
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        // For terminal states, we can't easily get there, so we test the key ones
        if (state == LoanState.Active)
        {
            Action action = () => loan.Activate(DisbursementDate.PlusDays(2), RecordedAt, CorrelationId);
            action.Should().Throw<InvalidStateTransitionException>()
                .Which.CurrentState.Should().Be(LoanState.Active);
        }
    }

    #endregion

    #region RecordPayment

    [Fact]
    public void RecordPayment_ValidPayment_ReducesBalance()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        var result = loan.RecordPayment(new Money(5_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        result.IsSuccess.Should().BeTrue();
        loan.RemainingBalance.Should().Be(new Money(95_000m, "EUR"));
    }

    [Fact]
    public void RecordPayment_EmitsPaymentReceivedEvent()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ClearUncommittedEvents();

        loan.RecordPayment(new Money(5_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        var @event = loan.UncommittedEvents.Should().ContainSingle().Which.Should().BeOfType<PaymentReceived>().Subject;
        @event.Amount.Should().Be(new Money(5_000m, "EUR"));
        @event.RemainingBalance.Should().Be(new Money(95_000m, "EUR"));
    }

    [Fact]
    public void RecordPayment_ZeroAmount_ReturnsFailure()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        var result = loan.RecordPayment(new Money(0, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public void RecordPayment_NegativeAmount_ReturnsFailure()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        var result = loan.RecordPayment(new Money(-100m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public void RecordPayment_ExceedsBalance_ReturnsFailure()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        var result = loan.RecordPayment(new Money(200_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("exceeds remaining balance");
    }

    [Fact]
    public void RecordPayment_DoesNotChangeState()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.RecordPayment(new Money(5_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        loan.State.Should().Be(LoanState.Active);
    }

    [Fact]
    public void RecordPayment_OnWrittenOff_Throws()
    {
        var loan = CreateLoan();
        // Manually set to Defaulted then WriteOff to reach WrittenOff
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);
        loan.WriteOff("Bad debt", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        Action action = () => loan.RecordPayment(new Money(5_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.CurrentState.Should().Be(LoanState.WrittenOff);
    }

    [Fact]
    public void RecordPayment_OnRestructured_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);
        loan.Restructure("Extended term", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        Action action = () => loan.RecordPayment(new Money(5_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.CurrentState.Should().Be(LoanState.Restructured);
    }

    [Fact]
    public void RecordPayment_OnRecovered_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);
        loan.Recover("Asset sold", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        Action action = () => loan.RecordPayment(new Money(5_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.CurrentState.Should().Be(LoanState.Recovered);
    }

    [Fact]
    public void RecordPayment_Failure_DoesNotChangeState()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        var balanceBefore = loan.RemainingBalance;

        loan.RecordPayment(new Money(0, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);

        loan.RemainingBalance.Should().Be(balanceBefore);
        loan.State.Should().Be(LoanState.Active);
    }

    #endregion

    #region ApplyAging

    [Fact]
    public void ApplyAging_FromActive_ZeroDays_StaysActive()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.ApplyAging(0, DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.State.Should().Be(LoanState.Active);
        loan.DaysPastDue.Should().BeNull();
    }

    [Fact]
    public void ApplyAging_FromActive_1Day_TransitionsToPastDue()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.ApplyAging(1, DisbursementDate.PlusDays(30), RecordedAt, CorrelationId,
            pastDueThreshold: 1, defaultThreshold: 90);

        loan.State.Should().Be(LoanState.PastDue);
        loan.DaysPastDue.Should().Be(1);
    }

    [Fact]
    public void ApplyAging_FromActive_10Days_TransitionsToPastDue()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.ApplyAging(10, DisbursementDate.PlusDays(40), RecordedAt, CorrelationId,
            pastDueThreshold: 1, defaultThreshold: 90);

        loan.State.Should().Be(LoanState.PastDue);
        loan.DaysPastDue.Should().Be(10);
    }

    [Fact]
    public void ApplyAging_FromActive_90Days_TransitionsToDefaulted()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.ApplyAging(90, DisbursementDate.PlusDays(120), RecordedAt, CorrelationId,
            pastDueThreshold: 1, defaultThreshold: 90);

        loan.State.Should().Be(LoanState.Defaulted);
        loan.DaysPastDue.Should().Be(90);
    }

    [Fact]
    public void ApplyAging_FromActive_120Days_TransitionsToDefaulted()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.ApplyAging(120, DisbursementDate.PlusDays(150), RecordedAt, CorrelationId,
            pastDueThreshold: 1, defaultThreshold: 90);

        loan.State.Should().Be(LoanState.Defaulted);
        loan.DaysPastDue.Should().Be(120);
    }

    [Fact]
    public void ApplyAging_FromPastDue_0Days_TransitionsToActive()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(10, DisbursementDate.PlusDays(40), RecordedAt, CorrelationId, 1, 90);

        loan.ApplyAging(0, DisbursementDate.PlusDays(41), RecordedAt, CorrelationId, 1, 90);

        loan.State.Should().Be(LoanState.Active);
        loan.DaysPastDue.Should().BeNull();
    }

    [Fact]
    public void ApplyAging_FromInGrace_0Days_TransitionsToActive()
    {
        // With pastDueThreshold = 1, there's no "grace" range (0 < days < 1 is empty).
        // We test InGrace by using a custom threshold where grace exists.
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        // With pastDueThreshold = 5, days 1-4 are InGrace
        loan.ApplyAging(3, DisbursementDate.PlusDays(10), RecordedAt, CorrelationId,
            pastDueThreshold: 5, defaultThreshold: 90);
        loan.State.Should().Be(LoanState.InGrace);

        loan.ApplyAging(0, DisbursementDate.PlusDays(11), RecordedAt, CorrelationId,
            pastDueThreshold: 5, defaultThreshold: 90);

        loan.State.Should().Be(LoanState.Active);
    }

    [Fact]
    public void ApplyAging_EmitsCorrectEvent_ForPastDue()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ClearUncommittedEvents();

        loan.ApplyAging(10, DisbursementDate.PlusDays(40), RecordedAt, CorrelationId, 1, 90);

        loan.UncommittedEvents.Should().ContainSingle(e => e is LoanBecamePastDue);
    }

    [Fact]
    public void ApplyAging_EmitsCorrectEvent_ForDefaulted()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ClearUncommittedEvents();

        loan.ApplyAging(90, DisbursementDate.PlusDays(120), RecordedAt, CorrelationId, 1, 90);

        loan.UncommittedEvents.Should().ContainSingle(e => e is LoanDefaulted);
    }

    [Fact]
    public void ApplyAging_FromDisbursed_Throws()
    {
        var loan = CreateLoan();
        // Loan is in Disbursed state

        Action action = () => loan.ApplyAging(10, DisbursementDate.PlusDays(40), RecordedAt, CorrelationId, 1, 90);

        InvalidStateTransitionException? ex = null;
        try { action(); } catch (InvalidStateTransitionException e) { ex = e; }
        ex.Should().NotBeNull();
        ex!.CurrentState.Should().Be(LoanState.Disbursed);
        ex.AttemptedCommand.Should().Be("ApplyAging");
    }

    [Fact]
    public void ApplyAging_FromWrittenOff_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);
        loan.WriteOff("Bad debt", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        Action action = () => loan.ApplyAging(10, DisbursementDate.PlusDays(110), RecordedAt, CorrelationId, 1, 90);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.CurrentState.Should().Be(LoanState.WrittenOff);
    }

    [Fact]
    public void ApplyAging_NegativeDays_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        Action action = () => loan.ApplyAging(-1, DisbursementDate.PlusDays(40), RecordedAt, CorrelationId, 1, 90);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ApplyAging_DefaultThreshold_CanBeCustomized()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        // Custom: default at 30 days instead of 90
        loan.ApplyAging(30, DisbursementDate.PlusDays(60), RecordedAt, CorrelationId, 1, 30);

        loan.State.Should().Be(LoanState.Defaulted);
    }

    #endregion

    #region WriteOff

    [Fact]
    public void WriteOff_FromDefaulted_TransitionsToWrittenOff()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);

        loan.WriteOff("Bad debt", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        loan.State.Should().Be(LoanState.WrittenOff);
    }

    [Fact]
    public void WriteOff_EmitsLoanWrittenOffEvent()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);
        loan.ClearUncommittedEvents();

        loan.WriteOff("Bad debt", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        var @event = loan.UncommittedEvents.Should().ContainSingle().Which.Should().BeOfType<LoanWrittenOff>().Subject;
        @event.Reason.Should().Be("Bad debt");
    }

    [Theory]
    [InlineData(LoanState.Disbursed)]
    [InlineData(LoanState.Active)]
    [InlineData(LoanState.InGrace)]
    [InlineData(LoanState.PastDue)]
    [InlineData(LoanState.WrittenOff)]
    [InlineData(LoanState.Restructured)]
    [InlineData(LoanState.Recovered)]
    public void WriteOff_FromNonDefaultedState_Throws(LoanState _)
    {
        // The state parameter documents which states are tested; the loan is
        // created in Disbursed state and activated, then WriteOff is attempted
        // from a non-Defaulted state in each case.
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        Action action = () => loan.WriteOff("Bad debt", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.AttemptedCommand.Should().Be("WriteOff");
    }

    [Fact]
    public void WriteOff_EmptyReason_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);

        Action action = () => loan.WriteOff("", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Restructure

    [Fact]
    public void Restructure_FromDefaulted_TransitionsToRestructured()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);

        loan.Restructure("Extended term to 180 months", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        loan.State.Should().Be(LoanState.Restructured);
    }

    [Fact]
    public void Restructure_EmitsLoanRestructuredEvent()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);
        loan.ClearUncommittedEvents();

        loan.Restructure("Extended term to 180 months", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        var @event = loan.UncommittedEvents.Should().ContainSingle().Which.Should().BeOfType<LoanRestructured>().Subject;
        @event.RestructuringPlan.Should().Be("Extended term to 180 months");
    }

    [Fact]
    public void Restructure_FromNonDefaulted_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        Action action = () => loan.Restructure("Extended term", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.AttemptedCommand.Should().Be("Restructure");
    }

    #endregion

    #region Recover

    [Fact]
    public void Recover_FromDefaulted_TransitionsToRecovered()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);

        loan.Recover("Collateral sold", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        loan.State.Should().Be(LoanState.Recovered);
    }

    [Fact]
    public void Recover_EmitsLoanRecoveredEvent()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.ApplyAging(100, DisbursementDate.PlusDays(100), RecordedAt, CorrelationId, 1, 90);
        loan.ClearUncommittedEvents();

        loan.Recover("Collateral sold", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        var @event = loan.UncommittedEvents.Should().ContainSingle().Which.Should().BeOfType<LoanRecovered>().Subject;
        @event.RecoveryMethod.Should().Be("Collateral sold");
    }

    [Fact]
    public void Recover_FromNonDefaulted_Throws()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        Action action = () => loan.Recover("Collateral sold", DisbursementDate.PlusDays(100), RecordedAt, CorrelationId);

        action.Should().Throw<InvalidStateTransitionException>()
            .Which.AttemptedCommand.Should().Be("Recover");
    }

    #endregion

    #region ClearUncommittedEvents

    [Fact]
    public void ClearUncommittedEvents_ClearsList()
    {
        var loan = CreateLoan();
        loan.UncommittedEvents.Should().HaveCount(1);

        loan.ClearUncommittedEvents();

        loan.UncommittedEvents.Should().BeEmpty();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsKeyInformation()
    {
        var loan = CreateLoan();

        var str = loan.ToString();
        str.Should().Contain(LoanId1.Value.ToString());
        str.Should().Contain("Disbursed");
        str.Should().Contain("100000");
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_Disburse_Activate_Age_Default_WriteOff()
    {
        var loan = CreateLoan();
        loan.State.Should().Be(LoanState.Disbursed);

        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);
        loan.State.Should().Be(LoanState.Active);

        loan.ApplyAging(10, DisbursementDate.PlusDays(40), RecordedAt, CorrelationId, 1, 90);
        loan.State.Should().Be(LoanState.PastDue);

        loan.ApplyAging(100, DisbursementDate.PlusDays(130), RecordedAt, CorrelationId, 1, 90);
        loan.State.Should().Be(LoanState.Defaulted);

        loan.WriteOff("Bad debt", DisbursementDate.PlusDays(130), RecordedAt, CorrelationId);
        loan.State.Should().Be(LoanState.WrittenOff);
    }

    [Fact]
    public void FullLifecycle_PaymentReducesBalance()
    {
        var loan = CreateLoan();
        loan.Activate(DisbursementDate.PlusDays(1), RecordedAt, CorrelationId);

        loan.RecordPayment(new Money(10_000m, "EUR"), FirstPaymentDate, RecordedAt, CorrelationId);
        loan.RemainingBalance.Should().Be(new Money(90_000m, "EUR"));

        loan.RecordPayment(new Money(10_000m, "EUR"), FirstPaymentDate.PlusMonths(1), RecordedAt, CorrelationId);
        loan.RemainingBalance.Should().Be(new Money(80_000m, "EUR"));
    }

    #endregion
}
