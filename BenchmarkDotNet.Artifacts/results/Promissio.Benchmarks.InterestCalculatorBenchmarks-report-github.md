```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8524)
Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 9.0.16 (9.0.1626.22923), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.16 (9.0.1626.22923), X64 RyuJIT AVX2


```
| Method                                       | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Calculate_SinglePeriod                       | 97.75 ns | 1.130 ns | 1.057 ns |  2.50 |    0.04 | 0.0021 |      40 B |        1.67 |
| Calculate_DayCountFraction_Actual360         | 39.13 ns | 0.524 ns | 0.437 ns |  1.00 |    0.02 | 0.0013 |      24 B |        1.00 |
| Calculate_DayCountFraction_Actual365         | 36.57 ns | 0.721 ns | 0.885 ns |  0.93 |    0.02 | 0.0013 |      24 B |        1.00 |
| Calculate_DayCountFraction_ActualActual      | 28.80 ns | 0.512 ns | 0.478 ns |  0.74 |    0.01 | 0.0013 |      24 B |        1.00 |
| Calculate_DayCountFraction_Thirty360         | 19.75 ns | 0.215 ns | 0.201 ns |  0.50 |    0.01 | 0.0013 |      24 B |        1.00 |
| Calculate_DayCountFraction_Thirty360European | 19.96 ns | 0.363 ns | 0.340 ns |  0.51 |    0.01 | 0.0013 |      24 B |        1.00 |
