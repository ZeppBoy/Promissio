using System;
using System.Collections.Generic;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates bullet payment schedules (principal paid in full at the end).
/// </summary>
public class BulletScheduleGenerator : IScheduleGenerator
{
    private readonly IInterestCalculator _interestCalculator;

    public BulletScheduleGenerator(IInterestCalculator interestCalculator)
    {
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

            // Interest on the full outstanding balance (bullet: balance never amortizes early)
            Money interestPortion = _interestCalculator.Calculate(
                new Money(remainingBalance, currency), interestRate, previousDate, paymentDate);

            // Bullet: principal stays at full balance until the balloon payment at maturity
            Money principalMoney = isLastAmortization
                ? new Money(remainingBalance, currency)
                : Money.Zero(currency);

            Money totalMoney = principalMoney + interestPortion;

            if (isLastAmortization)
            {
                remainingBalance = 0;
            }

            items.Add(new PaymentScheduleItem(
                i, paymentDate, principalMoney, interestPortion, totalMoney));

            previousDate = paymentDate;
        }

        return items;
    }
}
