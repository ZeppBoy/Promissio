using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates payment schedules from predefined cash flows.
/// Useful for loans with irregular payment patterns.
/// </summary>
public class CustomScheduleGenerator : IScheduleGenerator
{
    private readonly List<CustomCashFlow> _customFlows;

    public CustomScheduleGenerator(List<CustomCashFlow> customFlows, IInterestCalculator interestCalculator)
    {
        _customFlows = customFlows ?? throw new ArgumentNullException(nameof(customFlows));
        // interestCalculator is accepted for constructor parity with the other IScheduleGenerator
        // implementations (and DI registration) but is unused: custom flows are caller-supplied verbatim.
        _ = interestCalculator;
    }

    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        InterestRate interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0,
        HolidayCalendar? holidayCalendar = null)
    {
        if (_customFlows.Count != termMonths)
            throw new ArgumentException(
                $"Number of custom cash flows ({_customFlows.Count}) must equal termMonths ({termMonths}).",
                nameof(termMonths));

        var totalPrincipal = _customFlows.Sum(f => f.PrincipalPortion.Amount);
        if (Math.Abs(totalPrincipal - principal.Amount) > 0.01m)
            throw new ArgumentException(
                $"Sum of custom cash flow principal portions ({totalPrincipal}) must equal principal ({principal.Amount}).",
                nameof(principal));

        var items = new List<PaymentScheduleItem>(_customFlows.Count);

        for (int i = 0; i < _customFlows.Count; i++)
        {
            var flow = _customFlows[i];
            var paymentDate = startDate.PlusMonths(i + 1);
            var totalPayment = flow.PrincipalPortion + flow.InterestPortion;

            items.Add(new PaymentScheduleItem(
                i + 1, paymentDate, flow.PrincipalPortion, flow.InterestPortion, totalPayment));
        }

        return items;
    }

    public record CustomCashFlow(
        Money PrincipalPortion,
        Money InterestPortion);
}
