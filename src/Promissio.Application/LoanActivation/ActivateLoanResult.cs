using Promissio.Domain.Loan;

namespace Promissio.Application.LoanActivation;

/// <summary>Identifies the outcome of activating a persisted loan.</summary>
public enum ActivateLoanStatus
{
    /// <summary>The loan was activated and the event was persisted.</summary>
    Activated = 0,

    /// <summary>No authoritative stream exists for the requested loan.</summary>
    NotFound = 1,

    /// <summary>The stream changed after loading and the activation was not appended.</summary>
    ConcurrencyConflict = 2
}

/// <summary>Result of the persisted loan-activation workflow.</summary>
/// <param name="LoanId">The requested loan identity.</param>
/// <param name="Status">The typed workflow outcome.</param>
public sealed record ActivateLoanResult(LoanId LoanId, ActivateLoanStatus Status);
