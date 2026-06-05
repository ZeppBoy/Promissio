using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck;
using NodaTime;
using Promissio.Domain.ValueObjects;
using Promissio.Domain.ScheduleGeneration;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

public class ScheduleBalancePropertyTests
{
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
            var rate = new Percentage(interestRateFraction);
            var startDate = new LocalDate(2024, 1, 1);

            var generator = new AnnuityScheduleGenerator();
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
            var rate = new Percentage(interestRateFraction);
            var startDate = new LocalDate(2024, 1, 1);

            var generator = new DifferentiatedScheduleGenerator();
            var schedule = generator.Generate(principal, rate, termMonths, startDate, gracePeriodMonths);

            var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
            totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        }
    }
}