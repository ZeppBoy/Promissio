using System;
using System.Collections.Generic;
using Promissio.Domain.ValueObjects;
using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates an annuity payment schedule where each period's total payment is constant.
/// </summary>
public class AnnuityScheduleGenerator : IScheduleGenerator
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
        
        // The annuity formula: P = [r * PV] / [1 - (1 + r)^-n]
        // where:
        // P = periodic payment
        // r = periodic interest rate
        // PV = present value (principal)
        // n = total number of periods
        
        decimal annualRate = interestRate.Fraction;
        decimal monthlyRate = annualRate / 12m;
        
        int amortizationPeriods = termMonths - gracePeriodMonths;
        decimal p;

        if (monthlyRate == 0)
        {
            p = principal.Amount / amortizationPeriods;
        }
        else
        {
            p = (monthlyRate * principal.Amount) / (1m - (decimal)Math.Pow((double)(1m + monthlyRate), -amortizationPeriods));
        }
        
        decimal remainingPrincipal = principal.Amount;
        for (int i = 1; i <= termMonths; i++)
        {
            // Handle grace period: only interest is paid (or nothing, depending on business rules)
            // For this implementation, we assume interest-only during grace period.
            
            decimal interestPortion = remainingPrincipal * monthlyRate;
            decimal principalPortion;
            decimal totalPayment;

            if (i <= gracePeriodMonths)
            {
                principalPortion = 0;
                totalPayment = interestPortion;
            }
            else
            {
                principalPortion = p - interestPortion;
                
                // Ensure the last payment clears the remaining principal exactly
                if (i == termMonths)
                {
                    principalPortion = remainingPrincipal;
                    totalPayment = principalPortion + interestPortion;
                }
                else
                {
                    totalPayment = p;
                }
            }

            // Clamp principal portion to remaining principal to avoid overpayment
            if (principalPortion > remainingPrincipal)
            {
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
