using BenchmarkDotNet.Running;

namespace Promissio.Domain.Benchmarks;

/// <summary>
/// Entry point for BenchmarkDotNet benchmarks.
/// Run with: dotnet run --configuration Release
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DayCountFractionBenchmarks>();
        BenchmarkRunner.Run<InterestCalculationBenchmarks>();
    }
}
