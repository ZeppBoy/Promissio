using Promissio.Domain.Loan;

namespace Promissio.Application.LoanCreation;

/// <summary>
/// Identifies the outcome of an idempotent origination-to-servicing handoff.
/// </summary>
public enum LoanCreationStatus
{
    /// <summary>A new loan stream was created.</summary>
    Created = 0,

    /// <summary>An identical handoff had already created the returned loan.</summary>
    Existing = 1,

    /// <summary>The application was already handed off with different business terms.</summary>
    Conflict = 2
}

/// <summary>
/// Result of creating a servicing loan at confirmed disbursement.
/// </summary>
/// <param name="LoanId">The created or previously created loan identity.</param>
/// <param name="Status">The typed idempotency outcome.</param>
public sealed record LoanCreationResult(LoanId LoanId, LoanCreationStatus Status);
