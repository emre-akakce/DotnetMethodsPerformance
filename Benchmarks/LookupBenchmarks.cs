using BenchmarkDotNet.Attributes;
using PerformanceBenchmarks.Models;

namespace PerformanceBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class LookupBenchmarks
{
    private List<int> _vendorCodesList = null!;
    private HashSet<int> _vendorCodesHashSet = null!;
    private Dictionary<int, bool> _vendorCodesDictionary = null!;
    private int[] _searchCodes = null!;

    [Params(100, 500, 1_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        // Generate vendor codes
        _vendorCodesList = Enumerable.Range(1, Size).ToList();
        _vendorCodesHashSet = _vendorCodesList.ToHashSet();
        _vendorCodesDictionary = _vendorCodesList.ToDictionary(x => x, x => true);

        // Generate random search codes (50% existing, 50% non-existing)
        _searchCodes = Enumerable.Range(0, 100)
            .Select(i => random.Next(0, Size * 2))
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public int List_Contains()
    {
        int found = 0;
        foreach (var code in _searchCodes)
        {
            if (_vendorCodesList.Contains(code))
                found++;
        }
        return found;
    }

    [Benchmark]
    public int HashSet_Contains()
    {
        int found = 0;
        foreach (var code in _searchCodes)
        {
            if (_vendorCodesHashSet.Contains(code))
                found++;
        }
        return found;
    }

    [Benchmark]
    public int Dictionary_ContainsKey()
    {
        int found = 0;
        foreach (var code in _searchCodes)
        {
            if (_vendorCodesDictionary.ContainsKey(code))
                found++;
        }
        return found;
    }
}
