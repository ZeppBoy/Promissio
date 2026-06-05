using System;
using System.Collections.Generic;
using Promissio.Domain.ValueObjects;
using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates a custom payment schedule based on a predefined list of cash flows.
/// </summary>
public class CustomScheduleGenerator : IScheduleGenerator
{
    private readonly List<CustomCashFlow> _customFlows;

    public CustomScheduleGenerator(List<CustomCashFlow> customFlows)
    {
        _customFlows = customFlows ?? throw new ArgumentNullException(nameof(customFlows));
    }

    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        Percentage interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0)
    {
        // For a custom schedule, the provided cash flows override the standard logic.
        // However, we still need to ensure the schedule matches the requested term.
        
        var schedule = new List<PaymentScheduleItem>();
        
        for (int i = 0; i < _customFlows.Count; i++)
        {
            var flow = _customFlows[i];
            
            // Adjust period to be 1-based
            int period = i + 1;
            
            // Ensure we don't exceed the requested term
            if (period > termMonths) break;

            schedule.Add(new PaymentScheduleItem(
                period,
                startDate.PlusMonths(period),
                flow.PrincipalPortion,
                flow.InterestPortion,
                new Money(
                    flow.PrincipalPortion.Amount + flow.InterestPortion.Amount, 
                    principal.Currency
                )
            ));
        }

        return schedule;
    }

    public record CustomCashFlow(
        Money PrincipalPortion,
        Money InterestPortion);
}
