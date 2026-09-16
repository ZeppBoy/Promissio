namespace Promissio.Infrastructure.LoanPersistence;

/// <summary>
/// Records the unique origination-to-servicing handoff for one loan application.
/// </summary>
/// <remarks>
/// The document is inserted in the same Marten transaction as the initial loan
/// event stream. Its identifier is the application idempotency key.
/// </remarks>
public sealed record LoanCreationReceipt(Guid Id, Guid LoanId);
