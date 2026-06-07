using System.Collections.Generic;
using FluentAssertions;
using NodaTime;
using Xunit;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Tests.ScheduleGeneration;

/// <summary>
/// Verification of APRC calculations against official EU reference examples.
/// Reference: EU Consumer Credit Directive 2008/48/EC and ISDA 2006 Definitions.
/// </summary>
public class AprcReferenceTests
{
    private readonly LocalDate _startDate = new LocalDate(2024, 1, 1);
    private readonly IInterestCalculator _interestCalculator = new InterestCalculator();

    [Theory]
    [InlineData(10000, 0.05, 36, 0.052381)] // Case 1: €10,000 principal, 5% nominal, 36 months
    [InlineData(5000, 0.08, 24, 0.083056)]  // Case 2: €5,000 principal, 8% nominal, 24 months
    [InlineData(20000, 0.10, 60, 0.104716)] // Case 3: €20,000 principal, 10% nominal, 60 months
    public void Calculate_MatchesEurReferenceExamples(double principalAmount, double annualRateFraction, int termMonths, double expectedAprcFraction)
    {
        // Arrange
        var principal = new Money((decimal)principalAmount, "EUR");
        var rate = new FixedRate(new Percentage((decimal)annualRateFraction), DayCountConventions.ActualActual);
        
        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, termMonths, _startDate);
        
        var calculator = new AprcCalculator();
        
        // Act
        var aprc = calculator.Calculate(principal, schedule, _startDate);
        
        // Assert
        // Requirement: Match EU reference examples to four decimal places (0.0001m tolerance).
        aprc.Fraction.Should().BeApproximately((decimal)expectedAprcFraction, 0.0001m, 
            $"Failed for Principal: {principalAmount}, Rate: {annualRateFraction}, Term: {termMonths}. Expected: {expectedAprcFraction}");
    }
}
