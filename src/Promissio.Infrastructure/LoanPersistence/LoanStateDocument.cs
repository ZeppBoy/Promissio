using Promissio.Domain.Loan;

namespace Promissio.Infrastructure.LoanPersistence;

/// <summary>Stores the rebuildable current-state view of one loan stream.</summary>
public sealed record LoanStateDocument(Guid Id, LoanState State, int? DaysPastDue);
