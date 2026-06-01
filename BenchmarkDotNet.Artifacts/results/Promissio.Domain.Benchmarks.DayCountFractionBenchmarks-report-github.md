```

BenchmarkDotNet v0.14.0, macOS 26.5 (25F71) [Darwin 25.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 9.0.11 (9.0.1125.51716), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.11 (9.0.1125.51716), Arm64 RyuJIT AdvSIMD


```
| Method                      | Mean     | Error    | StdDev   | Rank | Allocated |
|---------------------------- |---------:|---------:|---------:|-----:|----------:|
| Calculate_Actual360         | 15.85 ns | 0.247 ns | 0.219 ns |    1 |         - |
| Calculate_Actual365         | 23.09 ns | 0.102 ns | 0.080 ns |    4 |         - |
| Calculate_Thirty360         | 18.09 ns | 0.359 ns | 0.300 ns |    2 |         - |
| Calculate_Thirty360European | 17.93 ns | 0.298 ns | 0.279 ns |    2 |         - |
| Calculate_ActualActual      | 21.61 ns | 0.103 ns | 0.086 ns |    3 |         - |
