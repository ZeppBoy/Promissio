using Promissio.Domain.Loan;

namespace Promissio.Application.LoanAging;

/// <summary>Identifies the outcome of applying aging to a persisted loan.</summary>
public enum ApplyLoanAgingStatus
{
    /// <summary>The aging transition was appended to the loan stream.</summary>
    Applied = 0,

    /// <summary>The aging input represented the documented Active-to-Active no-op.</summary>
    NoChange = 1,

    /// <summary>No authoritative stream exists for the requested loan.</summary>
    NotFound = 2,

    /// <summary>The stream changed after loading and the aging event was not appended.</summary>
    ConcurrencyConflict = 3
}

/// <summary>Result of the persisted loan-aging workflow.</summary>
/// <param name="LoanId">The requested loan identity.</param>
/// <param name="Status">The typed workflow outcome.</param>
/// <param name="State">The resulting state when the operation was applied or required no change.</param>
public sealed record ApplyLoanAgingResult(
    LoanId LoanId,
    ApplyLoanAgingStatus Status,
    LoanState? State);
