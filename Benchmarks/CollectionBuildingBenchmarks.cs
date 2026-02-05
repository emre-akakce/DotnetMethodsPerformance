using BenchmarkDotNet.Attributes;
using PerformanceBenchmarks.Models;

namespace PerformanceBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class CollectionBuildingBenchmarks
{
    private List<CardCampaign> _sourceCampaigns = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        _sourceCampaigns = Enumerable.Range(1, Size)
            .Select(i => new CardCampaign
            {
                Id = i,
                VendorCode = random.Next(1, Size / 100 + 1),
                CampaignName = $"Campaign {i}",
                DiscountPercentage = random.Next(5, 30),
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(30),
                IsActive = random.Next(0, 2) == 1
            })
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public List<CardCampaign> BuildList()
    {
        var result = new List<CardCampaign>();
        foreach (var campaign in _sourceCampaigns)
        {
            result.Add(campaign);
        }
        return result;
    }

    [Benchmark]
    public List<CardCampaign> BuildList_WithCapacity()
    {
        var result = new List<CardCampaign>(_sourceCampaigns.Count);
        foreach (var campaign in _sourceCampaigns)
        {
            result.Add(campaign);
        }
        return result;
    }

    [Benchmark]
    public HashSet<CardCampaign> BuildHashSet()
    {
        var result = new HashSet<CardCampaign>();
        foreach (var campaign in _sourceCampaigns)
        {
            result.Add(campaign);
        }
        return result;
    }

    [Benchmark]
    public HashSet<CardCampaign> BuildHashSet_WithCapacity()
    {
        var result = new HashSet<CardCampaign>(_sourceCampaigns.Count);
        foreach (var campaign in _sourceCampaigns)
        {
            result.Add(campaign);
        }
        return result;
    }

    [Benchmark]
    public Dictionary<int, CardCampaign> BuildDictionary()
    {
        var result = new Dictionary<int, CardCampaign>();
        foreach (var campaign in _sourceCampaigns)
        {
            result[campaign.VendorCode] = campaign;
        }
        return result;
    }

    [Benchmark]
    public Dictionary<int, CardCampaign> BuildDictionary_WithCapacity()
    {
        var result = new Dictionary<int, CardCampaign>(_sourceCampaigns.Count);
        foreach (var campaign in _sourceCampaigns)
        {
            result[campaign.VendorCode] = campaign;
        }
        return result;
    }

    [Benchmark]
    public Dictionary<int, List<CardCampaign>> BuildDictionary_Grouped()
    {
        var result = new Dictionary<int, List<CardCampaign>>();
        foreach (var campaign in _sourceCampaigns)
        {
            if (!result.ContainsKey(campaign.VendorCode))
            {
                result[campaign.VendorCode] = new List<CardCampaign>();
            }
            result[campaign.VendorCode].Add(campaign);
        }
        return result;
    }

    [Benchmark]
    public Dictionary<int, List<CardCampaign>> BuildDictionary_Grouped_LINQ()
    {
        return _sourceCampaigns
            .GroupBy(c => c.VendorCode)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
