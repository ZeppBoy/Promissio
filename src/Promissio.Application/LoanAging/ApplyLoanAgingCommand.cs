using MediatR;
using NodaTime;
using Promissio.Domain.Loan;

namespace Promissio.Application.LoanAging;

/// <summary>
/// Applies the approved delinquency aging rules to an authoritative loan stream.
/// </summary>
/// <param name="LoanId">The authoritative loan stream identity.</param>
/// <param name="DaysPastDue">The number of days the loan is past due.</param>
/// <param name="EffectiveDate">The business-effective aging date.</param>
/// <param name="RecordedAt">The instant at which aging was recorded.</param>
/// <param name="CorrelationId">The workflow correlation identifier.</param>
/// <param name="PastDueThreshold">The first day classified as past due.</param>
/// <param name="DefaultThreshold">The first day classified as defaulted.</param>
public sealed record ApplyLoanAgingCommand(
    LoanId LoanId,
    int DaysPastDue,
    LocalDate EffectiveDate,
    Instant RecordedAt,
    Guid CorrelationId,
    int PastDueThreshold = 1,
    int DefaultThreshold = 90) : IRequest<ApplyLoanAgingResult>;
