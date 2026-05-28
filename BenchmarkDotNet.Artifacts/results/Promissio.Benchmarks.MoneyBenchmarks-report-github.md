```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8524)
Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 9.0.16 (9.0.1626.22923), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.16 (9.0.1626.22923), X64 RyuJIT AVX2


```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Money_Addition       | 6.993 ns | 0.0737 ns | 0.0689 ns |  1.00 |    0.01 | 0.0021 |      40 B |        1.00 |
| Money_Multiplication | 6.346 ns | 0.0977 ns | 0.0914 ns |  0.91 |    0.02 | 0.0021 |      40 B |        1.00 |
| Money_Equality       | 7.426 ns | 0.0506 ns | 0.0473 ns |  1.06 |    0.01 | 0.0021 |      40 B |        1.00 |
