using System;
using System.Collections.Generic;
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
    private readonly IInterestCalculator _interestCalculator;

    public CustomScheduleGenerator(List<CustomCashFlow> customFlows, IInterestCalculator interestCalculator)
    {
        _customFlows = customFlows ?? throw new ArgumentNullException(nameof(customFlows));
        _interestCalculator = interestCalculator;
    }

    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        InterestRate interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0,
        HolidayCalendar? holidayCalendar = null)
    {
        if (gracePeriodMonths < 0)
            throw new ArgumentException("Grace period cannot be negative.", nameof(gracePeriodMonths));

        if (gracePeriodMonths >= termMonths)
            throw new ArgumentException("Grace period must be less than total term.", nameof(gracePeriodMonths));

        if (principal.Amount <= 0)
            throw new ArgumentException("Principal must be positive.", nameof(principal));

        if (interestRate.Rate.Fraction < 0)
            throw new ArgumentException("Interest rate must be non-negative.", nameof(interestRate));

        if (termMonths <= 0)
            throw new ArgumentException("Term must be positive.", nameof(termMonths));

        var currency = principal.Currency;

        var items = new List<PaymentScheduleItem>();
        decimal remainingBalance = principal.Amount;
        LocalDate previousDate = startDate;

        int amortizationPeriods = termMonths - gracePeriodMonths;
        decimal totalPayment;

        if (interestRate.Rate.Fraction == 0)
        {
            // Zero interest case - simple equal principal payments
            totalPayment = principal.Amount / amortizationPeriods;
        }
        else
        {
            // Custom formula: M = P * r * (1+r)^n / ((1+r)^n - 1)
            decimal rate = interestRate.Rate.Fraction / 12;
            decimal factor = DecimalPower(1m + rate, amortizationPeriods);
            totalPayment = principal.Amount * rate * factor / (factor - 1m);
        }

        // Count amortization periods to track which is the last one
        int amortizationCount = 0;

        for (int i = 1; i <= termMonths; i++)
        {
            var paymentDate = startDate.PlusMonths(i);

            if (i <= gracePeriodMonths)
            {
                // Grace period: interest only
                Money graceInterestPortion = _interestCalculator.Calculate(
                    new Money(remainingBalance, currency), interestRate, previousDate, paymentDate);

                items.Add(new PaymentScheduleItem(
                    i, paymentDate, Money.Zero(currency), graceInterestPortion, graceInterestPortion));

                previousDate = paymentDate;
                continue;
            }

            // Track amortization period number
            amortizationCount++;
            bool isLastAmortization = amortizationCount == amortizationPeriods;

            // Interest on current balance
            Money interestPortion = _interestCalculator.Calculate(
                new Money(remainingBalance, currency), interestRate, previousDate, paymentDate);

            // Calculate principal portion
            decimal principalPortion = totalPayment - interestPortion.Amount;

            // Last amortization period: pay remaining balance (absorbs all rounding)
            if (isLastAmortization)
            {
                principalPortion = remainingBalance;
            }
            else
            {
                // Ensure principal portion is non-negative and doesn't exceed remaining balance
                if (principalPortion < 0)
                {
                    principalPortion = 0m;
                }
                else if (principalPortion > remainingBalance)
                {
                    principalPortion = remainingBalance;
                }

                // Round principal portion to 2 decimal places
                principalPortion = Math.Round(principalPortion, 2, MidpointRounding.ToEven);
            }

            Money principalMoney = new Money(principalPortion, currency);
            Money totalMoney = principalMoney + interestPortion;

            // Update balance with rounded value
            remainingBalance -= principalPortion;

            // Safety check: ensure balance doesn't go negative from rounding
            if (remainingBalance < 0)
            {
                remainingBalance = 0;
            }

            items.Add(new PaymentScheduleItem(
                i, paymentDate, principalMoney, interestPortion, totalMoney));

            previousDate = paymentDate;
        }

        return items;
    }

    private static decimal DecimalPower(decimal baseValue, int exponent)
    {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
        {
            result *= baseValue;
        }
        return result;
    }

    public record CustomCashFlow(
        Money PrincipalPortion,
        Money InterestPortion);
}
