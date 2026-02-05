using BenchmarkDotNet.Attributes;
using PerformanceBenchmarks.Models;

namespace PerformanceBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class JoinOperationBenchmarks
{
    private List<Price> _prices = null!;
    private List<CardCampaign> _campaigns = null!;
    private Dictionary<int, List<CardCampaign>> _campaignsByVendor = null!;

    [Params(100, 500, 1_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        // Generate prices
        _prices = Enumerable.Range(1, Size)
            .Select(i => new Price
            {
                Id = i,
                ProductId = random.Next(1, Size / 10 + 1),
                VendorCode = random.Next(1, Size / 100 + 1),
                Amount = random.Next(10, 1000)
            })
            .ToList();

        // Generate campaigns (each vendor has 0-3 campaigns)
        _campaigns = Enumerable.Range(1, Size / 100 + 1)
            .SelectMany(vendorCode =>
            {
                var campaignCount = random.Next(0, 4);
                return Enumerable.Range(0, campaignCount)
                    .Select(i => new CardCampaign
                    {
                        Id = vendorCode * 10 + i,
                        VendorCode = vendorCode,
                        CampaignName = $"Campaign {i} for Vendor {vendorCode}",
                        DiscountPercentage = random.Next(5, 30),
                        StartDate = DateTime.Now.AddDays(-30),
                        EndDate = DateTime.Now.AddDays(30),
                        IsActive = true
                    });
            })
            .ToList();

        // Pre-build dictionary for optimized lookup
        _campaignsByVendor = _campaigns
            .GroupBy(c => c.VendorCode)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    [Benchmark(Baseline = true)]
    public List<Price> ManualLoop_WithDictionary()
    {
        var result = new List<Price>(_prices.Count);

        foreach (var price in _prices)
        {
            var priceWithCampaigns = new Price
            {
                Id = price.Id,
                ProductId = price.ProductId,
                VendorCode = price.VendorCode,
                Amount = price.Amount
            };

            if (_campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns))
            {
                priceWithCampaigns.CardCampaigns = new List<CardCampaign>(campaigns);
            }

            result.Add(priceWithCampaigns);
        }

        return result;
    }

    [Benchmark]
    public List<Price> ManualLoop_WithLinearSearch()
    {
        var result = new List<Price>(_prices.Count);

        foreach (var price in _prices)
        {
            var priceWithCampaigns = new Price
            {
                Id = price.Id,
                ProductId = price.ProductId,
                VendorCode = price.VendorCode,
                Amount = price.Amount
            };

            var campaigns = _campaigns.Where(c => c.VendorCode == price.VendorCode).ToList();
            if (campaigns.Count > 0)
            {
                priceWithCampaigns.CardCampaigns = campaigns;
            }

            result.Add(priceWithCampaigns);
        }

        return result;
    }

    [Benchmark]
    public List<Price> LINQ_Join()
    {
        return _prices
            .GroupJoin(
                _campaigns,
                price => price.VendorCode,
                campaign => campaign.VendorCode,
                (price, campaigns) => new Price
                {
                    Id = price.Id,
                    ProductId = price.ProductId,
                    VendorCode = price.VendorCode,
                    Amount = price.Amount,
                    CardCampaigns = campaigns.ToList()
                })
            .ToList();
    }

    [Benchmark]
    public List<Price> LINQ_Join_WithPreGroupedCampaigns()
    {
        return _prices
            .Select(price => new Price
            {
                Id = price.Id,
                ProductId = price.ProductId,
                VendorCode = price.VendorCode,
                Amount = price.Amount,
                CardCampaigns = _campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns)
                    ? new List<CardCampaign>(campaigns)
                    : new List<CardCampaign>()
            })
            .ToList();
    }

    [Benchmark]
    public List<Price> LINQ_SelectMany_Flatten()
    {
        var campaignsByPrice = _prices
            .SelectMany(
                price => _campaigns.Where(c => c.VendorCode == price.VendorCode).DefaultIfEmpty(),
                (price, campaign) => new { Price = price, Campaign = campaign })
            .GroupBy(x => x.Price.Id)
            .Select(g => new Price
            {
                Id = g.First().Price.Id,
                ProductId = g.First().Price.ProductId,
                VendorCode = g.First().Price.VendorCode,
                Amount = g.First().Price.Amount,
                CardCampaigns = g.Where(x => x.Campaign != null)
                    .Select(x => x.Campaign!)
                    .ToList()
            })
            .ToList();

        return campaignsByPrice;
    }
}
