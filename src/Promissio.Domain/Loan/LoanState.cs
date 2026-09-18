namespace Promissio.Domain.Loan;

/// <summary>
/// Represents the current state of a loan in its lifecycle.
/// </summary>
/// <remarks>
/// State names are defined by AGENTS.md §8 and confirmed by the owner on 2026-09-10.
/// Transition rules are documented in <c>docs/domain/loan-state-machine.md</c>.
/// </remarks>
public enum LoanState
{
    /// <summary>Loan has been disbursed; not yet in the active servicing cycle.</summary>
    Disbursed = 0,

    /// <summary>Loan is active and performing normally.</summary>
    Active = 1,

    /// <summary>Loan is within the grace period (days past due ≥ grace threshold but below default threshold).</summary>
    InGrace = 2,

    /// <summary>Loan is past due (days past due ≥ past-due threshold, default 1).</summary>
    PastDue = 3,

    /// <summary>Loan is in default (days past due ≥ default threshold, default 90).</summary>
    Defaulted = 4,

    /// <summary>Loan has been written off. Terminal state.</summary>
    WrittenOff = 5,

    /// <summary>Loan has been restructured. Terminal state (from the default perspective).</summary>
    Restructured = 6,

    /// <summary>Loan has been recovered. Terminal state.</summary>
    Recovered = 7
}
