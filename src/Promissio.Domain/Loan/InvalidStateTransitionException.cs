namespace Promissio.Domain.Loan;

/// <summary>
/// Thrown when a command is applied that is prohibited by the loan state-transition table.
/// </summary>
/// <remarks>
/// Per AGENTS.md §6, invalid transitions throw this exception with full context:
/// current state, attempted transition, and reason. Both failure paths (this exception
/// and <see cref="Result{T}"/> for business validation failures) leave aggregate state
/// unchanged and emit no events.
/// </remarks>
public sealed class InvalidStateTransitionException : Exception
{
    /// <summary>The state the loan was in when the invalid transition was attempted.</summary>
    public LoanState CurrentState { get; }

    /// <summary>The command that was attempted.</summary>
    public string AttemptedCommand { get; }

    /// <summary>Human-readable explanation of why this transition is not allowed.</summary>
    public string Reason { get; }

    public InvalidStateTransitionException(LoanState currentState, string attemptedCommand, string reason)
        : base($"Cannot apply '{attemptedCommand}' in state '{currentState}'. Reason: {reason}")
    {
        CurrentState = currentState;
        AttemptedCommand = attemptedCommand;
        Reason = reason;
    }
}
