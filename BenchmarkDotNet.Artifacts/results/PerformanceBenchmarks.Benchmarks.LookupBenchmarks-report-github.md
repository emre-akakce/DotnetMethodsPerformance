```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.7623)
12th Gen Intel Core i5-12400F, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.303
  [Host]     : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2


```
| Method                 | Size | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------- |----- |-----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| **List_Contains**          | **100**  |   **585.5 ns** | **11.70 ns** | **30.62 ns** |  **1.00** |    **0.00** |    **3** |         **-** |          **NA** |
| ⭐ HashSet_Contains       | 100  |   272.0 ns |  5.43 ns | 14.49 ns |  0.47 |    0.04 |    1 |         - |          NA |
| Dictionary_ContainsKey | 100  |   286.7 ns |  5.75 ns | 13.78 ns |  0.49 |    0.04 |    2 |         - |          NA |
|                        |      |            |          |          |       |         |      |           |             |
| **List_Contains**          | **500**  | **1,950.6 ns** | **31.76 ns** | **26.52 ns** |  **1.00** |    **0.00** |    **3** |         **-** |          **NA** |
| ⭐ HashSet_Contains       | 500  |   238.6 ns |  4.03 ns |  3.57 ns |  0.12 |    0.00 |    1 |         - |          NA |
| Dictionary_ContainsKey | 500  |   275.6 ns |  5.41 ns |  6.44 ns |  0.14 |    0.00 |    2 |         - |          NA |
|                        |      |            |          |          |       |         |      |           |             |
| **List_Contains**          | **1000** | **4,256.2 ns** | **84.05 ns** | **74.51 ns** |  **1.00** |    **0.00** |    **3** |         **-** |          **NA** |
| ⭐ HashSet_Contains       | 1000 |   263.0 ns |  5.20 ns |  7.46 ns |  0.06 |    0.00 |    1 |         - |          NA |
| Dictionary_ContainsKey | 1000 |   295.6 ns |  7.23 ns | 21.31 ns |  0.07 |    0.00 |    2 |         - |          NA |
