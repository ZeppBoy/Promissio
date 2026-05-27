using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;

namespace Promissio.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunMode.Evaluate)]
public class InterestCalculatorBenchmarks
{
    private IInterestCalculator _calculator!;
    private Money _principal!;
    private FixedRate _rate!;
    private DayCountConvention _convention!;
    private LocalDate _startDate!;
    private LocalDate _endDate!;

    [GlobalSetup]
    public void Setup()
    {
        _calculator = new InterestCalculator();
        _principal = new Money(100000m, "USD");
        _rate = new FixedRate(Percentage.FromPercent(5.5m), new Actual360());
        _convention = new Actual360();
        _startDate = new LocalDate(2023, 1, 1);
        _endDate = new LocalDate(2023, 7, 1);
    }

    [Benchmark]
    public Money Calculate_SinglePeriod()
    {
        return _calculator.Calculate(_principal, _rate, _convention, _startDate, _endDate);
    }

    [Benchmark(Baseline = true)]
    public Decimal Calculate_DayCountFraction_Actual360()
    {
        var convention = new Actual360();
        return convention.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public Decimal Calculate_DayCountFraction_Actual365()
    {
        var convention = new Actual365();
        return convention.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public Decimal Calculate_DayCountFraction_ActualActual()
    {
        var convention = new ActualActual();
        LocalDate start = new LocalDate(2023, 1, 1);
        LocalDate end = new LocalDate(2025, 1, 1);
        return convention.Fraction(start, end);
    }

    [Benchmark]
    public Decimal Calculate_DayCountFraction_Thirty360()
    {
        var convention = new Thirty360();
        return convention.Fraction(_startDate, _endDate);
    }

    [Benchmark]
    public Decimal Calculate_DayCountFraction_Thirty360European()
    {
        var convention = new Thirty360European();
        return convention.Fraction(_startDate, _endDate);
    }
}

[MemoryDiagnoser]
[SimpleJob(RunMode.Evaluate)]
public class MoneyBenchmarks
{
    private Money _a!;
    private Money _b!;

    [GlobalSetup]
    public void Setup()
    {
        _a = new Money(10000m, "USD");
        _b = new Money(5000m, "USD");
    }

    [Benchmark(Baseline = true)]
    public Money Money_Addition()
    {
        return _a + _b;
    }

    [Benchmark]
    public Money Money_Multiplication()
    {
        return _a * 2.5m;
    }

    [Benchmark]
    public bool Money_Equality()
    {
        var c = new Money(10000m, "USD");
        return _a == c;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<InterestCalculatorBenchmarks>();
        BenchmarkRunner.Run<MoneyBenchmarks>();
    }
}
