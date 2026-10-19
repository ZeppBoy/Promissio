using Promissio.Domain.Loan;

namespace Promissio.Application.LoanStateQueries;

/// <summary>Describes the current persisted lifecycle state of a loan.</summary>
public sealed record LoanStateView(LoanId LoanId, LoanState State, int? DaysPastDue);
