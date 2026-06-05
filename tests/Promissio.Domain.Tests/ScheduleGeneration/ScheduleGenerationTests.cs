using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NodaTime;
using Promissio.Domain.ValueObjects;
using Promissio.Domain.ScheduleGeneration;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

public class ScheduleGenerationTests
{
    private readonly LocalDate _startDate = new LocalDate(2024, 1, 1);

    [Fact]
    public void AnnuityGenerator_NoGrace_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.05m); // 5% annual
        var term = 12;

        // Act
        var generator = new AnnuityScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void AnnuityGenerator_WithGrace_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.05m);
        var term = 12;
        var grace = 3;

        // Act
        var generator = new AnnuityScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate, grace);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        
        // Verify grace period
        for (int i = 1; i <= grace; i++)
        {
            schedule.First(s => s.Period == i).PrincipalPortion.Amount.Should().Be(0);
        }
    }

    [Fact]
    public void DifferentiatedGenerator_NoGrace_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.05m);
        var term = 10;

        // Act
        var generator = new DifferentiatedScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void DifferentiatedGenerator_WithGrace_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.05m);
        var term = 10;
        var grace = 2;

        // Act
        var generator = new DifferentiatedScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate, grace);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }


    [Fact]
    public void BulletGenerator_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.05m);
        var term = 12;

        // Act
        var generator = new BulletScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
        
        // Principal should be 0 until the last period
        for (int i = 1; i < term; i++)
        {
            schedule.First(s => s.Period == i).PrincipalPortion.Amount.Should().Be(0);
        }
        schedule.First(s => s.Period == term).PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void CustomGenerator_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.05m);
        var term = 5;
        var customFlows = new List<CustomScheduleGenerator.CustomCashFlow>
        {
            new(new Money(1000, "USD"), new Money(500, "USD")),
            new(new Money(2000, "USD"), new Money(500, "USD")),
            new(new Money(3000, "USD"), new Money(500, "USD")),
            new(new Money(3000, "USD"), new Money(500, "USD")),
            new(new Money(1000, "USD"), new Money(500, "USD"))
        };

        // Act
        var generator = new CustomScheduleGenerator(customFlows);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(5);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void AprcCalculator_Annuity_MatchesReference()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.10m); // 10% annual
        var term = 12;
        
        var generator = new AnnuityScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate);
        
        var calculator = new AprcCalculator();

        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);

        // Assert
        // For 10% annual, monthly is ~0.8333%
        // PV of 12 payments of ~879.16 at 0.8333% is ~10000
        // APRC should be close to 10%
        aprc.Fraction.Should().BeApproximately(0.10m, 0.0001m);
    }

    [Fact]
    public void AprcCalculator_Annuity_MatchesReference_HighPrecision()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.10m); // 10% annual
        var term = 12;
        
        var generator = new AnnuityScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate);
        
        var calculator = new AprcCalculator();

        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);

        // Assert
        // For 10% annual interest on an annuity loan with no fees, 
        // the APRC should be exactly 10.0000%.
        aprc.Fraction.Should().BeApproximately(0.10m, 0.000001m);
    }

    [Fact]
    public void AprcCalculator_Bullet_MatchesReference()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.10m);
        var term = 12;
        
        var generator = new BulletScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate);
        
        var calculator = new AprcCalculator();

        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);

        // Assert
        // For a bullet loan, the APRC will be higher than the interest rate 
        // because the principal is returned at the end.
        aprc.Fraction.Should().BeGreaterThan(0.10m);
    }


    [Fact]
    public void ScheduleGenerators_RandomizedBalanceInvariant()
    {
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < 200; i++)
        {
            var principalAmount = random.Next(1000, 1000000);
            var annualRate = (decimal)(random.NextDouble() * 0.30); // 0% to 30%
            var term = random.Next(6, 360); // 6 months to 30 years
            var grace = random.Next(0, Math.Min(term - 1, 24));

            var principal = new Money(principalAmount, "USD");
            var rate = new Percentage(annualRate);

            var generators = new List<IScheduleGenerator>
            {
                new AnnuityScheduleGenerator(),
                new DifferentiatedScheduleGenerator(),
                new BulletScheduleGenerator()
            };

            foreach (var generator in generators)
            {
                var schedule = generator.Generate(principal, rate, term, _startDate, grace);
                var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
                totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m, $"Failed for Principal: {principalAmount}, Rate: {annualRate}, Term: {term}, Grace: {grace}");
            }
        }
    }
}