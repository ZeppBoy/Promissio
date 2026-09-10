using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Loan.Events;

/// <summary>
/// Emitted when a payment is received against the loan.
/// </summary>
/// <remarks>
/// The payment may cover interest, principal, or both. The remaining balance
/// after the payment is included so consumers can update projections without
/// querying the aggregate.
/// </remarks>
public sealed class PaymentReceived : LoanEvent
{
    public PaymentReceived(
        LoanId loanId,
        LocalDate effectiveDate,
        Instant recordedAt,
        Guid correlationId,
        ValueObjects.Money amount,
        ValueObjects.Money remainingBalance)
        : base(loanId, effectiveDate, recordedAt, correlationId)
    {
        Amount = amount;
        RemainingBalance = remainingBalance;
    }

    public ValueObjects.Money Amount { get; }
    public ValueObjects.Money RemainingBalance { get; }
}
