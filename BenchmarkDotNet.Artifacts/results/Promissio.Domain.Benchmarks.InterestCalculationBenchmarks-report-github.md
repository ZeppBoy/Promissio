```

BenchmarkDotNet v0.14.0, macOS 26.5 (25F71) [Darwin 25.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 9.0.11 (9.0.1125.51716), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.11 (9.0.1125.51716), Arm64 RyuJIT AdvSIMD


```
| Method             | Mean        | Error    | StdDev   | Rank | Gen0   | Allocated |
|------------------- |------------:|---------:|---------:|-----:|-------:|----------:|
| Calculate_Single   |    91.29 ns | 0.953 ns | 0.845 ns |    1 | 0.0191 |     160 B |
| Calculate_Segments | 1,351.08 ns | 2.286 ns | 1.909 ns |    2 | 0.2918 |    2456 B |
