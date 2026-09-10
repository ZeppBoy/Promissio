using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>Preserves caller-supplied principal, interest and optional payment dates.</summary>
/// <remarks>Grace periods are validated, never synthesized; explicit dates are not moved. See ADR-0004.</remarks>
public sealed class CustomScheduleGenerator : IScheduleGenerator
{
    private readonly CustomCashFlow[] _customFlows;

    /// <summary>Copies cash flows so subsequent changes to the caller's list cannot alter the schedule.</summary>
    /// <remarks>The calculator parameter is retained for source compatibility; custom interest is supplied by the caller.</remarks>
    public CustomScheduleGenerator(List<CustomCashFlow> customFlows, IInterestCalculator interestCalculator)
    {
        ArgumentNullException.ThrowIfNull(customFlows);
        ArgumentNullException.ThrowIfNull(interestCalculator);
        _customFlows = customFlows.ToArray();
    }

    /// <inheritdoc />
    public IEnumerable<PaymentScheduleItem> Generate(Money principal, InterestRate interestRate,
        int termMonths, LocalDate startDate, int gracePeriodMonths = 0,
        HolidayCalendar? holidayCalendar = null, LocalDate? firstPaymentDate = null)
    {
        ScheduleDates.Validate(principal, interestRate, termMonths, startDate, gracePeriodMonths, firstPaymentDate);
        if (_customFlows.Length != termMonths)
            throw new ArgumentException("Supply one cash flow per instalment.", nameof(termMonths));
        List<PaymentScheduleItem> items = [];
        Money totalPrincipal = Money.Zero(principal.Currency);
        LocalDate previousDate = startDate;
        for (int i = 0; i < _customFlows.Length; i++)
        {
            CustomCashFlow flow = _customFlows[i];
            ArgumentNullException.ThrowIfNull(flow);
            ArgumentNullException.ThrowIfNull(flow.PrincipalPortion);
            ArgumentNullException.ThrowIfNull(flow.InterestPortion);
            if (flow.PrincipalPortion.Currency != principal.Currency || flow.InterestPortion.Currency != principal.Currency)
                throw new ArgumentException("Cash-flow currencies must match the principal.");
            if (i < gracePeriodMonths && flow.PrincipalPortion.Amount != 0)
                throw new ArgumentException("Supplied grace instalments must be interest-only.", nameof(gracePeriodMonths));
            LocalDate contractualDate = flow.PaymentDate ?? ScheduleDates.Contractual(startDate, i + 1, firstPaymentDate);
            LocalDate payableDate = flow.PaymentDate ?? ScheduleDates.Payable(contractualDate, holidayCalendar);
            if (payableDate <= previousDate)
                throw new ArgumentException("Cash-flow dates must increase strictly after disbursement.");
            if (flow.PaymentDate.HasValue && holidayCalendar is not null && holidayCalendar.NextBusinessDay(payableDate) != payableDate)
                throw new ArgumentException("An explicit payment date must be a business day in the supplied calendar.");
            items.Add(new PaymentScheduleItem(i + 1, payableDate, flow.PrincipalPortion,
                flow.InterestPortion, flow.PrincipalPortion + flow.InterestPortion, contractualDate));
            totalPrincipal += flow.PrincipalPortion;
            previousDate = payableDate;
        }
        if (totalPrincipal != principal)
            throw new ArgumentException("Supplied principal portions must repay the principal exactly.");
        return items;
    }

    /// <summary>A supplied cash flow whose optional date takes precedence over monthly date generation.</summary>
    /// <param name="PrincipalPortion">Principal repaid at this instalment.</param>
    /// <param name="InterestPortion">Contractually supplied interest, preserved without recalculation.</param>
    /// <param name="PaymentDate">Explicit payment date, or null to use the monthly contractual date.</param>
    public sealed record CustomCashFlow(Money PrincipalPortion, Money InterestPortion, LocalDate? PaymentDate = null);
}
