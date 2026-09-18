using MediatR;
using NodaTime;
using Promissio.Domain.Loan;
using Promissio.Domain.ValueObjects;

namespace Promissio.Application.LoanActivation;

/// <summary>
/// Activates a disbursed loan on the first business day after disbursement.
/// </summary>
/// <param name="LoanId">The authoritative loan stream identity.</param>
/// <param name="EffectiveDate">The business-effective activation date.</param>
/// <param name="Calendar">The calendar used to determine the first business day.</param>
/// <param name="RecordedAt">The instant at which the activation was recorded.</param>
/// <param name="CorrelationId">The workflow correlation identifier.</param>
public sealed record ActivateLoanCommand(
    LoanId LoanId,
    LocalDate EffectiveDate,
    HolidayCalendar Calendar,
    Instant RecordedAt,
    Guid CorrelationId) : IRequest<ActivateLoanResult>;
