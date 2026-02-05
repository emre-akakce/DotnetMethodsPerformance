# Performance Benchmarks: Price Comparison App

This project contains comprehensive performance benchmarks for comparing different collection types and parallelization strategies in .NET, specifically designed for a price comparison application scenario where vendor card campaigns need to be mapped to product prices.

## Scenario

The application has:
- **Prices**: Each product has multiple prices from different vendors (identified by vendor codes)
- **Card Campaigns**: Each vendor may have multiple card campaigns
- **Goal**: Map the card campaigns to prices efficiently

## Benchmark Categories

### 1. Lookup Benchmarks
Compares lookup performance across different collection types:
- `List<T>.Contains()` - Linear search O(n)
- `HashSet<T>.Contains()` - Hash-based lookup O(1)
- `Dictionary<TKey, TValue>.ContainsKey()` - Hash-based lookup O(1)

**Test sizes**: 100, 1,000, 10,000, 100,000 items

### 2. Parallel Benchmarks
Compares sequential vs parallel processing for mapping campaigns to prices:
- `foreach` - Traditional sequential loop
- `Parallel.ForEach` - Parallel processing with thread-safe collection
- `PLINQ AsParallel()` - Parallel LINQ with default settings
- `PLINQ WithDegreeOfParallelism()` - Parallel LINQ with explicit CPU core usage

**Test sizes**: 100, 1,000, 10,000, 100,000 prices

### 3. Collection Building Benchmarks
Compares building different collection types:
- `List<T>` - Dynamic array (with and without initial capacity)
- `HashSet<T>` - Unique items collection (with and without initial capacity)
- `Dictionary<TKey, TValue>` - Key-value pairs (with and without initial capacity)
- Grouped dictionary (manual vs LINQ GroupBy)

**Test sizes**: 100, 1,000, 10,000, 100,000 items

### 4. Join Operation Benchmarks
Compares different approaches for joining prices with campaigns:
- Manual loop with Dictionary lookup (pre-grouped)
- Manual loop with linear search
- LINQ `GroupJoin`
- LINQ with pre-grouped campaigns
- LINQ `SelectMany` with grouping

**Test sizes**: 100, 1,000, 10,000, 100,000 prices

## Prerequisites

- .NET 8.0 SDK or later
- Windows, Linux, or macOS

## Running the Benchmarks

### Option 1: Interactive Menu
```bash
cd C:\Users\user\Documents\Workspace\dotnet\performance
dotnet run -c Release
```

### Option 2: Run Specific Benchmark
```bash
dotnet run -c Release -- lookup      # Run lookup benchmarks
dotnet run -c Release -- parallel    # Run parallel benchmarks
dotnet run -c Release -- collection  # Run collection building benchmarks
dotnet run -c Release -- join        # Run join operation benchmarks
```

### Option 3: Run All Benchmarks
```bash
dotnet run -c Release -- --all
```

## Important Notes

1. **Always run in Release mode** (`-c Release`) for accurate results
2. **Close other applications** to minimize interference
3. **Benchmarks take time** - each benchmark runs multiple iterations for statistical accuracy
4. **Results vary by hardware** - CPU, RAM, and system load affect results

## Understanding Results

BenchmarkDotNet will display:

- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Rank**: Relative ranking (1 = fastest)
- **Gen0/Gen1/Gen2**: Garbage collection counts per 1000 operations
- **Allocated**: Memory allocated per operation

### Example Output
```
|                Method |   Size |        Mean |     Error |    StdDev | Rank |  Allocated |
|---------------------- |------- |------------:|----------:|----------:|-----:|-----------:|
|      HashSet_Contains |    100 |    45.23 ns |  0.234 ns |  0.219 ns |    1 |          - |
| Dictionary_ContainsKey|    100 |    46.78 ns |  0.312 ns |  0.292 ns |    2 |          - |
|         List_Contains |    100 | 1,234.56 ns | 12.456 ns | 11.654 ns |    3 |          - |
```

## Expected Insights

### For Lookups:
- **Small datasets (< 1,000)**: List may be competitive
- **Large datasets (> 10,000)**: HashSet/Dictionary significantly faster

### For Parallel Processing:
- **Small datasets (< 1,000)**: Sequential often faster (overhead of parallelization)
- **Large datasets (> 10,000)**: Parallel processing shows benefits
- **PLINQ**: Generally cleaner code, competitive performance

### For Collection Building:
- **Pre-allocating capacity**: Significantly reduces allocations and improves performance
- **Dictionary grouping**: Manual loop typically faster than LINQ GroupBy for large datasets

### For Join Operations:
- **Pre-grouped Dictionary lookup**: Usually fastest for repeated access
- **LINQ GroupJoin**: Clean, readable, decent performance
- **Linear search**: Avoid for large datasets (O(n²) complexity)

## Results Location

Benchmark results are saved in:
```
BenchmarkDotNet.Artifacts/results/
```

Available formats:
- HTML reports
- Markdown tables
- CSV files for further analysis

## Customization

To modify test parameters, edit the `[Params]` attribute in benchmark classes:

```csharp
[Params(100, 1_000, 10_000, 100_000)]  // Modify these values
public int Size { get; set; }
```

## Real-World Recommendations

Based on typical results:

1. **For vendor code lookups**: Use `Dictionary<int, List<CardCampaign>>` for pre-grouped campaigns
2. **For mapping campaigns to prices**: Use sequential loop with dictionary lookup for < 10K items, consider PLINQ for larger datasets
3. **Always pre-allocate collection capacity** when size is known
4. **Profile your actual workload** - these benchmarks provide guidance, but real data patterns matter

## Troubleshooting

### Benchmark doesn't run
- Ensure you're using Release mode: `dotnet run -c Release`
- Check .NET SDK version: `dotnet --version`

### Results seem inconsistent
- Close background applications
- Run multiple times to verify consistency
- Check CPU throttling/power settings

### Out of memory errors
- Reduce the maximum `[Params]` size
- Increase available system memory

## Further Reading

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Tips](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
- [Parallel Programming in .NET](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/)
