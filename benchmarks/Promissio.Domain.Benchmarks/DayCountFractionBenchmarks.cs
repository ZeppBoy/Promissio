using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;

namespace Promissio.Domain.Benchmarks;

/// <summary>
/// Benchmarks for day-count fraction calculation performance.
/// These are hot paths called frequently in schedule generation.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class DayCountFractionBenchmarks
{
    private readonly Actual360 _actual360;
    private readonly Actual365 _actual365;
    private readonly Thirty360 _thirty360;
    private readonly Thirty360European _thirty360European;
    private readonly ActualActual _actualActual;

    private readonly LocalDate _startDate;
    private readonly LocalDate _endDate;

    public DayCountFractionBenchmarks()
    {
        _actual360 = new Actual360();
        _actual365 = new Actual365();
        _thirty360 = new Thirty360();
        _thirty360European = new Thirty360European();
        _actualActual = new ActualActual();

        _startDate = new LocalDate(2024, 1, 15);
        _endDate = new LocalDate(2024, 7, 31);
    }

    [Benchmark]
    public decimal Calculate_Actual360()
    {
        return _actual360.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public decimal Calculate_Actual365()
    {
        return _actual365.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public decimal Calculate_Thirty360()
    {
        return _thirty360.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public decimal Calculate_Thirty360European()
    {
        return _thirty360European.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public decimal Calculate_ActualActual()
    {
        return _actualActual.Fraction(_startDate, _endDate);
    }
}
