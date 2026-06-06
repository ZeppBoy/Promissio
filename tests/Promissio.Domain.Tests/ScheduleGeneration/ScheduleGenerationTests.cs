using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

public class ScheduleGenerationTests
{
    private readonly LocalDate _startDate = new LocalDate(2024, 1, 1);
    private readonly IInterestCalculator _interestCalculator = new InterestCalculator();

    [Fact]
    public void AnnuityGenerator_NoGrace_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.05m), DayCountConventions.ActualActual);
        var term = 12;

        // Act
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
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
        var rate = new FixedRate(new Percentage(0.05m), DayCountConventions.ActualActual);
        var term = 12;
        var grace = 3;

        // Act
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
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
        var rate = new FixedRate(new Percentage(0.05m), DayCountConventions.ActualActual);
        var term = 10;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
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
        var rate = new FixedRate(new Percentage(0.05m), DayCountConventions.ActualActual);
        var term = 10;
        var grace = 2;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
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
        var rate = new FixedRate(new Percentage(0.05m), DayCountConventions.ActualActual);
        var term = 12;

        // Act
        var generator = new BulletScheduleGenerator(_interestCalculator);
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
        var rate = new FixedRate(new Percentage(0.05m), DayCountConventions.ActualActual);
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
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;

        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        var calculator = new AprcCalculator();

        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);

        // Assert
        // For an annuity loan, the APRC is the effective annual rate, not the nominal rate.
        // The effective annual rate (EAR) = (1 + r/n)^n - 1 where r is nominal rate and n is compounding periods.
        // For 10% nominal with monthly compounding: (1 + 0.10/12)^12 - 1 ≈ 10.47%
        // This is because the APRC discounts each monthly payment individually.
        aprc.Fraction.Should().BeApproximately(0.104715723888028m, 0.0001m);
    }

    [Fact]
    public void AprcCalculator_Annuity_MatchesReference_HighPrecision()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;

        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        var calculator = new AprcCalculator();

        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);

        // Assert
        // For an annuity loan at 10% nominal with monthly compounding, 
        // the APRC (effective annual rate) is approximately 10.47%.
        // The exact value depends on the bisection method precision.
        // Tolerance of 0.00001m (0.001%) is sufficient for regulatory compliance.
        aprc.Fraction.Should().BeApproximately(0.104715723888028m, 0.00001m);
    }

    [Fact]
    public void AprcCalculator_Bullet_MatchesReference()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;

        var generator = new BulletScheduleGenerator(_interestCalculator);
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
            var grace = random.Next(0, Math.Max(1, term - 1));

            var principal = new Money(principalAmount, "USD");
            var rate = new FixedRate(new Percentage(annualRate), DayCountConventions.ActualActual);

            var generators = new List<IScheduleGenerator>
            {
                new AnnuityScheduleGenerator(_interestCalculator),
                new DifferentiatedScheduleGenerator(_interestCalculator),
                new BulletScheduleGenerator(_interestCalculator)
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
