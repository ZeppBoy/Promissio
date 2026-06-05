using System;
using System.Collections.Generic;
using Promissio.Domain.ValueObjects;
using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates a differentiated payment schedule where the principal portion is equal across all periods.
/// </summary>
public class DifferentiatedScheduleGenerator : IScheduleGenerator
{
    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        Percentage interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0)
    {
        if (gracePeriodMonths < 0 || gracePeriodMonths >= termMonths)
        {
            throw new ArgumentOutOfRangeException(nameof(gracePeriodMonths), "Grace period must be non-negative and less than the total term.");
        }

        var schedule = new List<PaymentScheduleItem>();
        
        decimal annualRate = interestRate.Fraction;
        decimal monthlyRate = annualRate / 12m;
        
        int amortizationPeriods = termMonths - gracePeriodMonths;
        decimal principalPortion = principal.Amount / amortizationPeriods;
        decimal remainingPrincipal = principal.Amount;
        
        for (int i = 1; i <= termMonths; i++)
        {
            decimal interestPortion = remainingPrincipal * monthlyRate;
            decimal currentPrincipalPortion;
            decimal totalPayment;

            if (i <= gracePeriodMonths)
            {
                currentPrincipalPortion = 0;
                totalPayment = interestPortion;
            }
            else
            {
                currentPrincipalPortion = principalPortion;
                
                // Ensure the last payment clears the remaining principal exactly
                if (i == termMonths)
                {
                    currentPrincipalPortion = remainingPrincipal;
                    totalPayment = currentPrincipalPortion + interestPortion;
                }
                else
                {
                    totalPayment = currentPrincipalPortion + interestPortion;
                }
            }

            // Clamp principal portion to remaining principal to avoid overpayment
            if (currentPrincipalPortion > remainingPrincipal)
            {
                currentPrincipalPortion = remainingPrincipal;
                totalPayment = currentPrincipalPortion + interestPortion;
            }

            remainingPrincipal -= currentPrincipalPortion;

            schedule.Add(new PaymentScheduleItem(
                i,
                startDate.PlusMonths(i),
                new Money(Math.Round(currentPrincipalPortion, 2), principal.Currency),
                new Money(Math.Round(interestPortion, 2), principal.Currency),
                new Money(Math.Round(totalPayment, 2), principal.Currency)
            ));
        }


        return schedule;
    }
}
