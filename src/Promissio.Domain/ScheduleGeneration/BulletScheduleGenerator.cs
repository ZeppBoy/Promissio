using System;
using System.Collections.Generic;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates bullet payment schedules (interest-only payments, full principal at maturity).
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

        for (int i = 1; i <= termMonths; i++)
        {
            var paymentDate = startDate.PlusMonths(i);

            // Interest on current balance
            Money interestPortion = _interestCalculator.Calculate(
                new Money(remainingBalance, currency), interestRate, previousDate, paymentDate);

            Money principalPortion = Money.Zero(currency);

            // Last period: pay full remaining principal + interest
            if (i == termMonths)
            {
                principalPortion = new Money(Math.Round(remainingBalance, 2, MidpointRounding.ToEven), currency);
                remainingBalance -= remainingBalance;
            }

            Money totalPayment = principalPortion + interestPortion;

            items.Add(new PaymentScheduleItem(
                i, paymentDate, principalPortion, interestPortion, totalPayment));

            previousDate = paymentDate;
        }

        return items;
    }
}
