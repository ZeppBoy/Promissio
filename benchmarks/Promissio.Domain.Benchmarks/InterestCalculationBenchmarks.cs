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
    private readonly Percentage _rate;
    private readonly Actual360 _convention;
    private readonly LocalDate _startDate;
    private readonly LocalDate _endDate;

    public InterestCalculationBenchmarks()
    {
        _calculator = new InterestCalculator();
        _principal = new Money(100000m, "USD");
        _rate = Percentage.FromPercent(5m);
        _convention = new Actual360();
        _startDate = new LocalDate(2024, 1, 1);
        _endDate = new LocalDate(2024, 12, 31);
    }

    [Benchmark]
    public InterestCalculationResult Calculate_Single()
    {
        var parameters = new InterestCalculationParameters(
            _principal, _rate, _convention, _startDate, _endDate);
        return _calculator.Calculate(parameters);
    }

    [Benchmark]
    public List<InterestCalculationResult> Calculate_Segments()
    {
        var segments = GenerateMonthlySegments(12);
        return _calculator.CalculateSegments(segments).ToList();
    }

    private IEnumerable<InterestCalculationParameters> GenerateMonthlySegments(int months)
    {
        var startDate = new LocalDate(2024, 1, 1);
        for (int i = 0; i < months; i++)
        {
            var endDate = startDate.PlusMonths(1);
            yield return new InterestCalculationParameters(
                _principal, _rate, _convention, startDate, endDate);
            startDate = endDate;
        }
    }
}
