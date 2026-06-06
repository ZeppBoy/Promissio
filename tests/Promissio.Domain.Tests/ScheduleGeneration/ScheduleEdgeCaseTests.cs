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

public class ScheduleEdgeCaseTests
{
    private readonly LocalDate _startDate = new LocalDate(2024, 1, 1);
    private readonly IInterestCalculator _interestCalculator = new InterestCalculator();

    [Fact]
    public void AnnuityGenerator_SinglePeriod_PaysFullPrincipal()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.12m), DayCountConventions.ActualActual);
        var term = 1;

        // Act
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(1);
        var item = schedule.First();
        item.Period.Should().Be(1);
        item.PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
        item.TotalPayment.Amount.Should().BeGreaterThan(principal.Amount);
    }

    [Fact]
    public void DifferentiatedGenerator_SinglePeriod_PaysFullPrincipal()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.12m), DayCountConventions.ActualActual);
        var term = 1;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(1);
        var item = schedule.First();
        item.Period.Should().Be(1);
        item.PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void BulletGenerator_SinglePeriod_PaysFullPrincipal()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.12m), DayCountConventions.ActualActual);
        var term = 1;

        // Act
        var generator = new BulletScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(1);
        var item = schedule.First();
        item.Period.Should().Be(1);
        item.PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void AnnuityGenerator_ZeroInterest_PaysEqualPrincipal()
    {
        // Arrange
        var principal = new Money(12000, "USD");
        var rate = new FixedRate(new Percentage(0m), DayCountConventions.ActualActual);
        var term = 12;

        // Act
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(12);
        var expectedPrincipal = Math.Round(principal.Amount / term, 2, MidpointRounding.ToEven);
        foreach (var item in schedule)
        {
            item.PrincipalPortion.Amount.Should().BeApproximately(expectedPrincipal, 0.01m);
            item.InterestPortion.Amount.Should().Be(0);
        }
    }

    [Fact]
    public void DifferentiatedGenerator_ZeroInterest_PaysEqualPrincipal()
    {
        // Arrange
        var principal = new Money(12000, "USD");
        var rate = new FixedRate(new Percentage(0m), DayCountConventions.ActualActual);
        var term = 12;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(12);
        var expectedPrincipal = Math.Round(principal.Amount / term, 2, MidpointRounding.ToEven);
        foreach (var item in schedule)
        {
            item.PrincipalPortion.Amount.Should().BeApproximately(expectedPrincipal, 0.01m);
            item.InterestPortion.Amount.Should().Be(0);
        }
    }

    [Fact]
    public void BulletGenerator_ZeroInterest_PaysOnlyPrincipalAtEnd()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0m), DayCountConventions.ActualActual);
        var term = 12;

        // Act
        var generator = new BulletScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(12);
        for (int i = 1; i < term; i++)
        {
            schedule.First(s => s.Period == i).PrincipalPortion.Amount.Should().Be(0);
            schedule.First(s => s.Period == i).InterestPortion.Amount.Should().Be(0);
        }
        schedule.First(s => s.Period == term).PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
        schedule.First(s => s.Period == term).InterestPortion.Amount.Should().Be(0);
    }

    [Fact]
    public void AnnuityGenerator_FullGrace_EqualToBullet()
    {
        // When grace period equals term-1, annuity should behave like bullet (all principal at end)
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;
        var grace = term - 1;

        // Act
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate, grace);

        // Assert
        schedule.Should().HaveCount(term);
        for (int i = 1; i <= grace; i++)
        {
            schedule.First(s => s.Period == i).PrincipalPortion.Amount.Should().Be(0);
        }
        schedule.First(s => s.Period == term).PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void DifferentiatedGenerator_FullGrace_EqualToBullet()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;
        var grace = term - 1;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate, grace);

        // Assert
        schedule.Should().HaveCount(term);
        for (int i = 1; i <= grace; i++)
        {
            schedule.First(s => s.Period == i).PrincipalPortion.Amount.Should().Be(0);
        }
        schedule.First(s => s.Period == term).PrincipalPortion.Amount.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void AnnuityGenerator_VeryLongTerm_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(500000, "USD");
        var rate = new FixedRate(new Percentage(0.06m), DayCountConventions.ActualActual);
        var term = 360; // 30 year mortgage

        // Act
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void DifferentiatedGenerator_VeryLongTerm_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(500000, "USD");
        var rate = new FixedRate(new Percentage(0.06m), DayCountConventions.ActualActual);
        var term = 360;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void BulletGenerator_VeryLongTerm_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(500000, "USD");
        var rate = new FixedRate(new Percentage(0.06m), DayCountConventions.ActualActual);
        var term = 360;

        // Act
        var generator = new BulletScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void AnnuityGenerator_NegativeTerm_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, -1, _startDate));
    }

    [Fact]
    public void DifferentiatedGenerator_NegativeTerm_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, -1, _startDate));
    }

    [Fact]
    public void BulletGenerator_NegativeTerm_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new BulletScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, -1, _startDate));
    }

    [Fact]
    public void AnnuityGenerator_NegativeGrace_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, 12, _startDate, -1));
    }

    [Fact]
    public void BulletGenerator_NegativeGrace_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new BulletScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, 12, _startDate, -1));
    }

    [Fact]
    public void BulletGenerator_GraceExceedsTerm_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new BulletScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, 6, _startDate, 12));
    }

    [Fact]
    public void AnnuityGenerator_GraceExceedsTerm_Throws()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, 6, _startDate, 12));
    }

    [Fact]
    public void AnnuityGenerator_ZeroPrincipal_Throws()
    {
        // Arrange
        var principal = Money.Zero("USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, 12, _startDate));
    }

    [Fact]
    public void BulletGenerator_ZeroPrincipal_Throws()
    {
        // Arrange
        var principal = Money.Zero("USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);

        // Act & Assert
        var generator = new BulletScheduleGenerator(_interestCalculator);
        Assert.Throws<ArgumentException>(() => generator.Generate(principal, rate, 12, _startDate));
    }

    [Fact]
    public void AnnuityGenerator_VeryHighInterest_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.50m), DayCountConventions.ActualActual); // 50% annual
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
    public void DifferentiatedGenerator_VeryHighInterest_ProducesBalancedSchedule()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.50m), DayCountConventions.ActualActual); // 50% annual
        var term = 12;

        // Act
        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        // Assert
        schedule.Should().HaveCount(term);
        var totalPrincipal = schedule.Sum(s => s.PrincipalPortion.Amount);
        totalPrincipal.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void PaymentScheduleItem_NegativePrincipalPortion_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PaymentScheduleItem(
            1, new LocalDate(2024, 2, 1), new Money(-100, "USD"), new Money(50, "USD"), new Money(-50, "USD")));
    }

    [Fact]
    public void PaymentScheduleItem_NegativeInterestPortion_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PaymentScheduleItem(
            1, new LocalDate(2024, 2, 1), new Money(100, "USD"), new Money(-50, "USD"), new Money(50, "USD")));
    }

    [Fact]
    public void PaymentScheduleItem_TotalMismatch_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PaymentScheduleItem(
            1, new LocalDate(2024, 2, 1), new Money(100, "USD"), new Money(50, "USD"), new Money(100, "USD")));
    }

    [Fact]
    public void PaymentScheduleItem_ZeroPeriod_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PaymentScheduleItem(
            0, new LocalDate(2024, 2, 1), new Money(100, "USD"), new Money(50, "USD"), new Money(150, "USD")));
    }

    [Fact]
    public void AprcCalculator_VeryLongSchedule_Computes()
    {
        // Arrange
        var principal = new Money(500000, "USD");
        var rate = new FixedRate(new Percentage(0.06m), DayCountConventions.ActualActual);
        var term = 360;

        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate);

        var calculator = new AprcCalculator();

        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);

        // Assert
        aprc.Fraction.Should().BeGreaterThan(0);
        aprc.Fraction.Should().BeApproximately(rate.Rate.Fraction, 0.01m);
    }
}
