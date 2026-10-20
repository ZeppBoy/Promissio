using NodaTime;
using Promissio.Domain.Loan;
using Promissio.Domain.ValueObjects;

namespace Promissio.Application.LoanQueries;

/// <summary>Describes a loan at the latest committed stream version.</summary>
/// <remarks>
/// Original principal is the amount at disbursement, not an outstanding balance.
/// This view follows stream order and does not answer historical business-date queries.
/// </remarks>
/// <param name="Id">The loan stream identity.</param>
/// <param name="LoanApplicationId">The originating application identity.</param>
/// <param name="TermsVersion">The approved and accepted terms version.</param>
/// <param name="OriginalPrincipal">The immutable principal at disbursement.</param>
/// <param name="DisbursementDate">The confirmed disbursement business date.</param>
/// <param name="FirstPaymentDate">The agreed first payment business date.</param>
/// <param name="State">The latest committed lifecycle state.</param>
/// <param name="DaysPastDue">The last recorded aging value, or null when cleared or unavailable.</param>
/// <param name="StreamVersion">The last event version represented by this view.</param>
public sealed record LoanSummary(
    Guid Id,
    Guid LoanApplicationId,
    int TermsVersion,
    Money OriginalPrincipal,
    LocalDate DisbursementDate,
    LocalDate FirstPaymentDate,
    LoanState State,
    int? DaysPastDue,
    long StreamVersion);
