using System;
using System.Collections.Generic;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates differentiated payment schedules (equal principal portions, decreasing total payments).
/// </summary>
public class DifferentiatedScheduleGenerator : IScheduleGenerator
{
    private readonly IInterestCalculator _interestCalculator;

    public DifferentiatedScheduleGenerator(IInterestCalculator interestCalculator)
    {
        _interestCalculator = interestCalculator;
    }

    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        InterestRate interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0)
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
        decimal principalPortion = principal.Amount / amortizationPeriods;

        // Track amortization periods
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

            decimal principalPortionForPeriod = principalPortion;

            // Last period: absorb rounding error - pay remaining balance
            if (isLastAmortization)
            {
                principalPortionForPeriod = remainingBalance;
            }

            // Ensure principal doesn't exceed remaining balance
            if (principalPortionForPeriod > remainingBalance)
            {
                principalPortionForPeriod = remainingBalance;
            }

            // Round principal portion to 2 decimal places
            var roundedPrincipalPortion = Math.Round(principalPortionForPeriod, 2, MidpointRounding.ToEven);

            Money principalMoney = new Money(roundedPrincipalPortion, currency);
            Money totalMoney = principalMoney + interestPortion;

            // Update balance with rounded value
            remainingBalance -= roundedPrincipalPortion;

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
}
