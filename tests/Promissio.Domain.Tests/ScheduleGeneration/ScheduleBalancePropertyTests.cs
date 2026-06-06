using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

public class ScheduleBalancePropertyTests
{
    private readonly IInterestCalculator _interestCalculator = new InterestCalculator();

    [Fact]
    public void AnnuityScheduleBalance_Randomized()
    {
        var random = new Random(42);
        for (int i = 0; i < 200; i++)
        {
            var principalAmount = random.Next(1000, 1000000);
            var interestRateFraction = (decimal)(random.NextDouble() * 0.30);
            var termMonths = random.Next(6, 360);
            var gracePeriodMonths = random.Next(0, Math.Min(termMonths - 1, 24));

            if (principalAmount <= 0 || interestRateFraction < -0.99m || interestRateFraction > 5.0m ||
                termMonths <= 0 || termMonths > 600 || gracePeriodMonths < 0 || gracePeriodMonths >= termMonths)
            {
                continue;
            }

            var principal = new Money(principalAmount, "USD");
            var rate = new FixedRate(new Percentage(interestRateFraction), DayCountConventions.ActualActual);
            var startDate = new LocalDate(2024, 1, 1);

            var generator = new AnnuityScheduleGenerator(_interestCalculator);
            var schedule = generator.Generate(principal, rate, termMonths, startDate, gracePeriodMonths);

            var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
            totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        }
    }

    [Fact]
    public void DifferentiatedScheduleBalance_Randomized()
    {
        var random = new Random(43);
        for (int i = 0; i < 200; i++)
        {
            var principalAmount = random.Next(1000, 1000000);
            var interestRateFraction = (decimal)(random.NextDouble() * 0.30);
            var termMonths = random.Next(6, 360);
            var gracePeriodMonths = random.Next(0, Math.Min(termMonths - 1, 24));

            if (principalAmount <= 0 || interestRateFraction < -0.99m || interestRateFraction > 5.0m ||
                termMonths <= 0 || termMonths > 600 || gracePeriodMonths < 0 || gracePeriodMonths >= termMonths)
            {
                continue;
            }

            var principal = new Money(principalAmount, "USD");
            var rate = new FixedRate(new Percentage(interestRateFraction), DayCountConventions.ActualActual);
            var startDate = new LocalDate(2024, 1, 1);

            var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
            var schedule = generator.Generate(principal, rate, termMonths, startDate, gracePeriodMonths);

            var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
            totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        }
    }

    [Fact]
    public void BulletScheduleBalance_Randomized()
    {
        var random = new Random(44);
        for (int i = 0; i < 200; i++)
        {
            var principalAmount = random.Next(1000, 1000000);
            var interestRateFraction = (decimal)(random.NextDouble() * 0.30);
            var termMonths = random.Next(6, 360);
            var gracePeriodMonths = random.Next(0, Math.Min(termMonths - 1, 24));

            if (principalAmount <= 0 || interestRateFraction < -0.99m || interestRateFraction > 5.0m ||
                termMonths <= 0 || termMonths > 600 || gracePeriodMonths < 0 || gracePeriodMonths >= termMonths)
            {
                continue;
            }

            var principal = new Money(principalAmount, "USD");
            var rate = new FixedRate(new Percentage(interestRateFraction), DayCountConventions.ActualActual);
            var startDate = new LocalDate(2024, 1, 1);

            var generator = new BulletScheduleGenerator(_interestCalculator);
            var schedule = generator.Generate(principal, rate, termMonths, startDate, gracePeriodMonths);

            var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
            totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        }
    }

    [Fact]
    public void CustomScheduleBalance_Randomized()
    {
        var random = new Random(45);
        for (int i = 0; i < 200; i++)
        {
            var principalAmount = random.Next(1000, 1000000);
            var interestRateFraction = (decimal)(random.NextDouble() * 0.30);
            var termMonths = random.Next(6, 60);

            if (principalAmount <= 0 || interestRateFraction < -0.99m || interestRateFraction > 5.0m ||
                termMonths <= 0)
            {
                continue;
            }

            var principal = new Money(principalAmount, "USD");
            var rate = new FixedRate(new Percentage(interestRateFraction), DayCountConventions.ActualActual);
            var startDate = new LocalDate(2024, 1, 1);

            // Generate equal principal portions for custom schedule
            var equalPortion = Math.Round(principal.Amount / termMonths, 2, MidpointRounding.ToEven);
            var flows = new List<Promissio.Domain.ScheduleGeneration.CustomScheduleGenerator.CustomCashFlow>();

            for (int j = 0; j < termMonths; j++)
            {
                var portion = (j == termMonths - 1)
                    ? principal.Amount - flows.Sum(f => f.PrincipalPortion.Amount)
                    : equalPortion;
                portion = Math.Round(portion, 2, MidpointRounding.ToEven);
                flows.Add(new Promissio.Domain.ScheduleGeneration.CustomScheduleGenerator.CustomCashFlow(
                    new Money(portion, "USD"),
                    new Money(0m, "USD")));
            }

            var generator = new CustomScheduleGenerator(flows);
            var schedule = generator.Generate(principal, rate, termMonths, startDate);

            var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
            totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        }
    }
}
