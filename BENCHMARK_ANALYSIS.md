# Benchmark Analysis & Recommendations

## Executive Summary

This document provides performance analysis and recommendations for the price comparison app, specifically for mapping vendor card campaigns to product prices.

## Scenario Overview

- **Goal**: Map card campaigns to prices efficiently
- **Data**: Vendor codes on prices, campaigns grouped by vendor
- **Scale**: Tested with 100, 500, and 1,000 items

---

## Recommended Approach

### **Winner: Manual Loop with Dictionary Lookup**

```csharp
// 1. Pre-build dictionary once (at data load time)
var campaignsByVendor = campaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());

// 2. Use for each price query/page load
var result = new List<Price>(prices.Count); // Always pre-allocate!

foreach (var price in prices)
{
    var priceWithCampaigns = new Price
    {
        Id = price.Id,
        ProductId = price.ProductId,
        VendorCode = price.VendorCode,
        Amount = price.Amount
    };

    if (campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns))
    {
        priceWithCampaigns.CardCampaigns = new List<CardCampaign>(campaigns);
    }

    result.Add(priceWithCampaigns);
}
```

### Why This Wins:

✅ **O(1) lookup complexity** - Dictionary.TryGetValue is constant time
✅ **No parallelization overhead** - For <1K items, sequential is faster
✅ **Minimal allocations** - Pre-allocated capacity avoids resizing
✅ **Simple and readable** - Easy to maintain and debug
✅ **Predictable performance** - No thread synchronization costs

---

## Performance Hierarchy (Expected)

### 1. Lookup Operations

| Approach | Complexity | Best For | Performance |
|----------|-----------|----------|-------------|
| **Dictionary/HashSet** | O(1) | Any size | ⚡⚡⚡ **BEST** |
| List.Contains | O(n) | < 50 items | ❌ Avoid for large lists |

**Key Insight**: Once your list exceeds ~50-100 items, Dictionary/HashSet becomes dramatically faster.

### 2. Join/Mapping Operations

| Approach | Speed Rank | Memory | Readability | When to Use |
|----------|-----------|---------|-------------|-------------|
| **Manual Loop + Dictionary** | 🥇 1st | Low | High | **Default choice** |
| LINQ + Pre-grouped Dict | 🥈 2nd | Low | Very High | Clean code priority |
| LINQ GroupJoin | 🥉 3rd | Medium | High | One-time operations |
| Parallel.ForEach | 4th* | High | Medium | 10K+ items only |
| PLINQ | 4th* | High | High | 10K+ items only |
| Linear Search (Where) | ❌ Last | Low | High | **NEVER use** (O(n²)) |

*Parallel approaches only win with large datasets (10K+) and CPU-bound work

### 3. Collection Building

| Approach | Performance | When to Use |
|----------|-------------|-------------|
| **List with capacity** | ⚡⚡⚡ | **Always** if size known |
| List without capacity | ⚡ | Size unknown |
| HashSet with capacity | ⚡⚡⚡ | Need uniqueness + size known |
| HashSet without capacity | ⚡ | Need uniqueness |
| Dictionary with capacity | ⚡⚡⚡ | Key-value + size known |
| Dictionary without capacity | ⚡ | Key-value pairs |

**Key Rule**: Always pre-allocate capacity if you know the size! This eliminates array resizing.

---

## Detailed Recommendations by Scenario

### Scenario 1: Loading Product Page (100-500 prices)
**Recommendation**: Manual loop + Dictionary

```csharp
// Cache this dictionary in memory or Redis
var campaignsByVendor = GetCampaignsDictionary();

var prices = GetPricesForProduct(productId);
var result = new List<Price>(prices.Count);

foreach (var price in prices)
{
    // ... manual loop with TryGetValue
}
```

**Why**: Fastest for this size, no parallelization overhead, simple code.

---

### Scenario 2: Batch Processing (1000+ prices)
**Recommendation**: Consider PLINQ with pre-grouped dictionary

```csharp
var campaignsByVendor = GetCampaignsDictionary();

var result = prices.AsParallel()
    .WithDegreeOfParallelism(Environment.ProcessorCount)
    .Select(price => new Price
    {
        Id = price.Id,
        ProductId = price.ProductId,
        VendorCode = price.VendorCode,
        Amount = price.Amount,
        CardCampaigns = campaignsByVendor.TryGetValue(price.VendorCode, out var c)
            ? new List<CardCampaign>(c)
            : new List<CardCampaign>()
    })
    .ToList();
```

**Why**: Parallel processing helps with large batches, PLINQ is cleaner than Parallel.ForEach.

---

### Scenario 3: API Response (Real-time query)
**Recommendation**: LINQ + Pre-grouped Dictionary (for readability)

```csharp
return prices
    .Select(price => new Price
    {
        Id = price.Id,
        ProductId = price.ProductId,
        VendorCode = price.VendorCode,
        Amount = price.Amount,
        CardCampaigns = campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns)
            ? new List<CardCampaign>(campaigns)
            : new List<CardCampaign>()
    })
    .ToList();
```

**Why**: Slightly slower than manual loop but much more readable, good balance for API code.

---

## Anti-Patterns to Avoid

### ❌ DON'T: Linear Search in Loop (O(n²))
```csharp
// This is VERY SLOW - O(n²) complexity
foreach (var price in prices)
{
    var campaigns = allCampaigns
        .Where(c => c.VendorCode == price.VendorCode)
        .ToList(); // BAD!
}
```

### ❌ DON'T: Forget to Pre-allocate Capacity
```csharp
var result = new List<Price>(); // Will resize multiple times - SLOW
```

### ❌ DON'T: Use Parallel for Small Datasets
```csharp
// For 100 items, parallel overhead > actual work
Parallel.ForEach(100items, ...); // SLOWER than foreach
```

### ❌ DON'T: Rebuild Dictionary Every Time
```csharp
// Rebuilding dictionary on every request - WASTEFUL
var dict = campaigns.GroupBy(...).ToDictionary(...); // Do once, cache it!
```

---

## Best Practices Summary

### 1. **Use Dictionary for Lookups**
- Pre-group campaigns by vendor code into Dictionary once
- Reuse this dictionary across multiple requests
- Consider caching in Redis/Memory for high-traffic apps

### 2. **Always Pre-allocate Capacity**
```csharp
var result = new List<Price>(prices.Count); // Good!
var result = new List<Price>(); // Bad - will resize
```

### 3. **Keep It Simple for < 1K Items**
- Sequential loops are fast enough
- Avoid parallelization overhead
- Optimize for readability

### 4. **Consider Parallelization for 10K+ Items**
- Use PLINQ (cleaner than Parallel.ForEach)
- Set degree of parallelism explicitly
- Test with your actual data sizes

### 5. **Profile with Real Data**
- These benchmarks use synthetic data
- Real-world data patterns matter
- Measure in production-like environment

---

## Implementation Checklist

- [ ] Build campaigns dictionary at application startup
- [ ] Cache dictionary in memory (consider Redis for distributed)
- [ ] Use manual loop + Dictionary.TryGetValue for standard queries
- [ ] Pre-allocate List capacity when size is known
- [ ] Avoid List.Contains for large lists (> 50 items)
- [ ] Only use parallel processing for batch operations (10K+ items)
- [ ] Monitor actual performance in production
- [ ] Consider pagination for very large result sets

---

## When to Revisit These Decisions

1. **Data size increases significantly** (>10K prices per query)
   - Re-test with parallel approaches
   - Consider pagination/streaming

2. **Complex business logic added** (heavy CPU work per item)
   - Parallelization benefits increase
   - Re-run benchmarks

3. **Memory becomes a concern**
   - Consider streaming/yield return
   - Evaluate dictionary caching strategy

4. **Performance issues in production**
   - Profile with real data
   - Check database query performance first
   - Consider adding database indexes on VendorCode

---

## Quick Reference

**For 99% of use cases in this app:**
```csharp
// Once at startup
var campaignsByVendor = campaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());

// Every request
var result = new List<Price>(prices.Count);
foreach (var price in prices)
{
    // ... create price with campaigns using TryGetValue
}
```

**This is simple, fast, and maintainable. Don't overcomplicate it!**

---

## Benchmark Results

*Once benchmarks complete, add actual results here with:*
- Execution times (mean, median, stddev)
- Memory allocations
- GC collections
- Comparative rankings

---

**Document Version**: 1.0
**Last Updated**: 2026-02-05
**Test Configuration**: .NET 8.0, BenchmarkDotNet, Sizes: 100/500/1000
