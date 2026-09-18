using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>Solves the dated present-value equation for a single drawdown and known non-negative repayments.</summary>
/// <remarks>
/// Uses the monthly, annual or weekly interval rules in SWD(2012)128, section 4.1.1.
/// Source: https://www.mfcr.cz/assets/attachments/EU-MFCR_Metodika_2012_128-Guidelines-consumer-credit-directive-swd-en.pdf
/// This is a known-cash-flow calculator, not an implementation of every regulatory product assumption.
/// </remarks>
public sealed class AprcCalculator : IAprcCalculator
{
    private readonly AprcPeriodUnit _periodUnit;

    /// <summary>Creates a calculator with an explicit repayment frequency, defaulting to monthly schedules.</summary>
    public AprcCalculator(AprcPeriodUnit periodUnit = AprcPeriodUnit.Months)
    {
        if (!Enum.IsDefined(periodUnit))
            throw new ArgumentOutOfRangeException(nameof(periodUnit));
        _periodUnit = periodUnit;
    }

    /// <inheritdoc />
    /// <remarks>Compatibility wrapper for trusted inputs; use TryCalculate for expected input or convergence failures.</remarks>
    public Percentage Calculate(Money principal, IEnumerable<PaymentScheduleItem> schedule,
        LocalDate disbursementDate, int maxIterations = 100)
    {
        AprcCalculationResult result = TryCalculate(principal, schedule, disbursementDate, maxIterations);
        return result.Value ?? throw new ArgumentException(result.Error, nameof(schedule));
    }

    /// <inheritdoc />
    public AprcCalculationResult TryCalculate(Money principal, IEnumerable<PaymentScheduleItem> schedule,
        LocalDate disbursementDate, int maxIterations = 100)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(schedule);
        if (principal.Amount <= 0 || maxIterations <= 0)
            return AprcCalculationResult.Failure("Principal and iteration limit must be positive.");
        if (disbursementDate.Calendar != CalendarSystem.Iso || disbursementDate.Year <= -9998)
            return AprcCalculationResult.Failure("An ISO date with a representable preceding year is required.");
        PaymentScheduleItem[] payments = schedule.ToArray();
        if (payments.Length == 0)
            return AprcCalculationResult.Failure("At least one repayment is required.");
        if (payments.Any(p => p is null || p.TotalPayment.Currency != principal.Currency
            || p.PaymentDate.Calendar != CalendarSystem.Iso || p.PaymentDate < disbursementDate
            || p.TotalPayment.Amount < 0))
            return AprcCalculationResult.Failure("Repayments must be non-negative, in the principal currency and on or after disbursement.");
        if (!payments.Any(p => p.PaymentDate > disbursementDate && p.TotalPayment.Amount > 0))
            return AprcCalculationResult.Failure("A positive repayment after disbursement is required.");

        // Normalize before summation; double is used only for fractional powers and root search,
        // never for posting money. This avoids decimal power overflow on long schedules.
        (double Amount, double Years)[] flows = payments.Select(p =>
            ((double)p.TotalPayment.Amount / (double)principal.Amount,
                AprcTime.Years(disbursementDate, p.PaymentDate, _periodUnit))).ToArray();
        double zeroValue = PresentValue(flows, 0);
        if (zeroValue < 1d - 1e-14)
            return AprcCalculationResult.Failure("A negative APRC is outside the non-negative Percentage contract.");
        if (Math.Abs(zeroValue - 1d) <= 1e-14)
            return AprcCalculationResult.Success(Percentage.FromFraction(0));
        if (flows.Where(f => f.Years == 0).Sum(f => f.Amount) >= 1d)
            return AprcCalculationResult.Failure("No finite non-negative rate balances these immediate charges.");

        double low = 0;
        double high = 1;
        while (PresentValue(flows, high) > 1d)
        {
            high *= 2;
            if (high >= (double)decimal.MaxValue)
                return AprcCalculationResult.Failure("The rate cannot be represented by Percentage.");
        }
        for (int i = 0; i < maxIterations; i++)
        {
            double mid = low + (high - low) / 2;
            double value = PresentValue(flows, mid);
            if (Math.Abs(value - 1d) <= 1e-12 && high - low <= 1e-10 * Math.Max(1d, mid))
                return AprcCalculationResult.Success(Percentage.FromFraction((decimal)mid));
            if (value > 1d)
                low = mid;
            else
                high = mid;
        }
        return AprcCalculationResult.Failure("APRC did not converge within the iteration limit.");
    }

    private static double PresentValue((double Amount, double Years)[] flows, double rate)
    {
        double sum = 0;
        foreach ((double amount, double years) in flows)
            sum += amount * Math.Exp(-years * Math.Log(1d + rate));
        return sum;
    }
}
