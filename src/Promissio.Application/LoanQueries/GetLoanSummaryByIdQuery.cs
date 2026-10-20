using MediatR;
using Promissio.Domain.Loan;

namespace Promissio.Application.LoanQueries;

/// <summary>Requests the latest committed summary of a loan.</summary>
/// <param name="LoanId">The loan to read.</param>
public sealed record GetLoanSummaryByIdQuery(LoanId LoanId) : IRequest<LoanSummary?>;
