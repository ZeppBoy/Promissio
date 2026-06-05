using System.Collections.Generic;
using FluentAssertions;
using NodaTime;
using Promissio.Domain.ValueObjects;
using Promissio.Domain.ScheduleGeneration;
using Verify;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

public class ScheduleSnapshotTests
{
    private readonly LocalDate _startDate = new LocalDate(2024, 1, 1);

    [Fact]
    public void AnnuitySchedule_CanonicalCase_MatchesSnapshot()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new Percentage(0.10m); // 10% annual
        var term = 12;

        var generator = new AnnuityScheduleGenerator();
        var schedule = generator.Generate(principal, rate, term, _startDate).ToList();

        // Act & Assert
        schedule.Verify();
    }
}