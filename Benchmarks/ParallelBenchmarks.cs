using BenchmarkDotNet.Attributes;
using PerformanceBenchmarks.Models;

namespace PerformanceBenchmarks.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ParallelBenchmarks
{
    private List<Price> _prices = null!;
    private Dictionary<int, List<CardCampaign>> _campaignsByVendor = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        // Generate prices with vendor codes
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
        _campaignsByVendor = Enumerable.Range(1, Size / 100 + 1)
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
            .GroupBy(c => c.VendorCode)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    [Benchmark(Baseline = true)]
    public List<Price> Foreach_Sequential()
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
    public List<Price> ParallelForEach()
    {
        var result = new List<Price>(_prices.Count);
        var lockObj = new object();

        Parallel.ForEach(_prices, price =>
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

            lock (lockObj)
            {
                result.Add(priceWithCampaigns);
            }
        });

        return result;
    }

    [Benchmark]
    public List<Price> PLINQ_AsParallel()
    {
        return _prices.AsParallel()
            .Select(price =>
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

                return priceWithCampaigns;
            })
            .ToList();
    }

    [Benchmark]
    public List<Price> PLINQ_WithDegreeOfParallelism()
    {
        return _prices.AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(price =>
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

                return priceWithCampaigns;
            })
            .ToList();
    }
}
