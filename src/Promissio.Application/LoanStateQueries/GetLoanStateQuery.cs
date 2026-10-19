using MediatR;
using Promissio.Domain.Loan;

namespace Promissio.Application.LoanStateQueries;

/// <summary>Requests the current projected lifecycle state for a loan.</summary>
public sealed record GetLoanStateQuery(LoanId LoanId) : IRequest<LoanStateView?>;
