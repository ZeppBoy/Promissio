using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Benchmarks;

/// <summary>
/// Benchmarks for interest calculation performance.
/// Measures the complete calculation pipeline including validation.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class InterestCalculationBenchmarks
{
    private readonly InterestCalculator _calculator;
    private readonly Money _principal;
    private readonly FixedRate _rate;
    private readonly Actual360 _convention;
    private readonly LocalDate _startDate;
    private readonly LocalDate _endDate;

    public InterestCalculationBenchmarks()
    {
        _calculator = new InterestCalculator();
        _principal = new Money(100000m, "USD");
        _convention = new Actual360();
        _rate = new FixedRate(Percentage.FromPercent(5m), _convention);
        _startDate = new LocalDate(2024, 1, 1);
        _endDate = new LocalDate(2024, 12, 31);
    }

    [Benchmark]
    public Money Calculate_Single()
    {
        return _calculator.Calculate(_principal, _rate, _startDate, _endDate);
    }

    [Benchmark]
    public IReadOnlyList<Money> Calculate_Segments()
    {
        var segments = GenerateMonthlySegments(12);
        return _calculator.CalculateForPeriods(_principal, _rate, segments.ToArray());
    }

    private IEnumerable<(LocalDate StartDate, LocalDate EndDate)> GenerateMonthlySegments(int months)
    {
        var startDate = new LocalDate(2024, 1, 1);
        for (int i = 0; i < months; i++)
        {
            var endDate = startDate.PlusMonths(1);
            yield return (startDate, endDate);
            startDate = endDate;
        }
    }
}
