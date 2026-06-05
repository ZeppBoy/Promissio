using System;
using System.Collections.Generic;
using Promissio.Domain.ValueObjects;
using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates a bullet payment schedule where interest is paid periodically and principal is paid in a single balloon payment at the end.
/// </summary>
public class BulletScheduleGenerator : IScheduleGenerator
{
    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        Percentage interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0)
    {
        var schedule = new List<PaymentScheduleItem>();
        
        decimal annualRate = interestRate.Fraction;
        decimal monthlyRate = annualRate / 12m;
        
        decimal remainingPrincipal = principal.Amount;
        
        for (int i = 1; i <= termMonths; i++)
        {
            decimal interestPortion = remainingPrincipal * monthlyRate;
            decimal principalPortion;
            decimal totalPayment;

            if (i <= gracePeriodMonths)
            {
                principalPortion = 0;
                totalPayment = interestPortion;
            }
            else if (i < termMonths)
            {
                principalPortion = 0;
                totalPayment = interestPortion;
            }
            else
            {
                // Final balloon payment
                principalPortion = remainingPrincipal;
                totalPayment = principalPortion + interestPortion;
            }

            remainingPrincipal -= principalPortion;

            schedule.Add(new PaymentScheduleItem(
                i,
                startDate.PlusMonths(i),
                new Money(Math.Round(principalPortion, 2), principal.Currency),
                new Money(Math.Round(interestPortion, 2), principal.Currency),
                new Money(Math.Round(totalPayment, 2), principal.Currency)
            ));
        }

        return schedule;
    }
}
