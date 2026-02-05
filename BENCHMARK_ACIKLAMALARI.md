# Benchmark Sınıfları Açıklamaları

Bu doküman, projede bulunan tüm benchmark sınıflarının ne yaptığını ve neden önemli olduklarını açıklamaktadır.

---

## 1. LookupBenchmarks.cs - Arama Performans Testleri

### Ne Yapar?
Bir satıcı kodunun bir koleksiyonda ne kadar hızlı bulunabileceğini test eder.

### Test Senaryosu:
- Satıcı kodlarından oluşan koleksiyonlar oluşturur (100, 500, 1000 öğe)
- Her koleksiyonda 100 rastgele satıcı kodu arar
- Karşılaştırma: `List.Contains()` vs `HashSet.Contains()` vs `Dictionary.ContainsKey()`

### Gerçek Dünya Kullanımı:
```csharp
// "Bu satıcının kampanyası var mı?" kontrolü
if (vendorCodes.Contains(price.VendorCode))
{
    // Kampanyaları göster
}
```

### Neden Önemli?
- Bu kontrolü bir ürün sayfasındaki HER fiyat için yapıyorsunuz
- Bir üründe 50-200 fiyat olabilir
- Yanlış seçim = sayfa yavaş yüklenir

### Test Edilen Yöntemler:

#### `List_Contains()` (Baseline)
```csharp
if (_vendorCodesList.Contains(code))
    found++;
```
- **Karmaşıklık**: O(n) - Linear arama
- **Ne Zaman İyi**: Sadece çok küçük listeler (< 50 öğe)
- **Sorun**: Liste büyüdükçe her aramada tüm listeyi tarar

#### `HashSet_Contains()`
```csharp
if (_vendorCodesHashSet.Contains(code))
    found++;
```
- **Karmaşıklık**: O(1) - Hash tablosu araması
- **Ne Zaman İyi**: Her zaman, özellikle > 100 öğe
- **Avantaj**: Boyut artsa da arama süresi sabit kalır

#### `Dictionary_ContainsKey()`
```csharp
if (_vendorCodesDictionary.ContainsKey(code))
    found++;
```
- **Karmaşıklık**: O(1) - Hash tablosu araması
- **Ne Zaman İyi**: Hem varlık kontrolü hem veri almanız gerektiğinde
- **Avantaj**: HashSet gibi hızlı + ilişkili veri saklayabilir

### Sonuç:
HashSet/Dictionary, 500-1000 öğe için List'ten 8-16x daha hızlı.

---

## 2. ParallelBenchmarks.cs - Paralel İşlem Testleri

### Ne Yapar?
Birden fazla fiyatı aynı anda işlemenin farklı yollarını test eder (sıralı vs paralel).

### Test Senaryosu:
- Bir fiyat listesi alır (100, 500, 1000)
- Her fiyat için, satıcısının kampanyalarını dictionary'den arar
- Kampanyalar eklenmiş yeni Price nesneleri oluşturur
- Karşılaştırma:
  - Düz `foreach` döngüsü (sıralı)
  - `Parallel.ForEach` (kilitlemeyle paralel)
  - `PLINQ AsParallel()` (paralel LINQ)
  - `PLINQ WithDegreeOfParallelism` (CPU çekirdek kontrolüyle paralel LINQ)

### Gerçek Dünya Kullanımı:
```csharp
// Seçenek 1: Sıralı
foreach (var price in prices)
{
    price.Campaigns = GetCampaignsForVendor(price.VendorCode);
}

// Seçenek 2: Paralel (birden fazla CPU çekirdeği kullanır)
Parallel.ForEach(prices, price =>
{
    price.Campaigns = GetCampaignsForVendor(price.VendorCode);
});
```

### Neden Önemli?
- Birden fazla CPU çekirdeği kullanmanın işleri hızlandırıp hızlandırmadığını belirler
- Paralel işlemin maliyeti var (thread oluşturma, senkronizasyon)
- Küçük veri setlerinde, maliyet kazançtan fazla olabilir

### Test Edilen Yöntemler:

#### `Foreach_Sequential()` (Baseline)
```csharp
var result = new List<Price>(_prices.Count);
foreach (var price in _prices)
{
    var priceWithCampaigns = new Price { ... };
    if (_campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns))
    {
        priceWithCampaigns.CardCampaigns = new List<CardCampaign>(campaigns);
    }
    result.Add(priceWithCampaigns);
}
```
- **Nasıl Çalışır**: Tek thread, fiyatları birer birer işler
- **Avantaj**: Basit, thread maliyeti yok
- **Dezavantaj**: Tüm CPU çekirdeklerini kullanmaz

#### `ParallelForEach()`
```csharp
var lockObj = new object();
Parallel.ForEach(_prices, price =>
{
    var priceWithCampaigns = new Price { ... };
    // ... kampanyaları ekle
    lock (lockObj)
    {
        result.Add(priceWithCampaigns);
    }
});
```
- **Nasıl Çalışır**: Birden fazla thread, lock ile sonuç listesine ekler
- **Avantaj**: CPU çekirdeklerini kullanır
- **Dezavantaj**: Lock yüzünden contention, küçük işler için overhead

#### `PLINQ_AsParallel()`
```csharp
return _prices.AsParallel()
    .Select(price => { ... })
    .ToList();
```
- **Nasıl Çalışır**: LINQ sorgusu paralel çalışır
- **Avantaj**: Temiz kod, lock gerekmez
- **Dezavantaj**: Küçük veri setlerinde overhead

#### `PLINQ_WithDegreeOfParallelism()`
```csharp
return _prices.AsParallel()
    .WithDegreeOfParallelism(Environment.ProcessorCount)
    .Select(price => { ... })
    .ToList();
```
- **Nasıl Çalışır**: PLINQ ama kaç thread kullanılacağı açıkça belirtilmiş
- **Avantaj**: Thread sayısı kontrolü
- **Ne Zaman**: Sistem kaynaklarını optimize etmek istediğinizde

### Beklenen Sonuç:
- Sıralı < 1000 öğe için kazanır (paralel overhead yok)
- Paralel 1000+ öğe için yardımcı olabilir, EĞER öğe başına iş önemliyse
- Sizin durumunuzda, dictionary araması o kadar hızlı ki paralel overhead muhtemelen değmez

---

## 3. CollectionBuildingBenchmarks.cs - Koleksiyon Oluşturma Testleri

### Ne Yapar?
Sıfırdan farklı koleksiyon tiplerini ne kadar hızlı oluşturabileceğinizi test eder.

### Test Senaryosu:
- Bir kampanya listesi alır
- Farklı koleksiyonlar oluşturur (List, HashSet, Dictionary)
- Kapasite önceden ayrılmış/ayrılmamış durumları karşılaştırır
- Manuel döngü vs LINQ ile gruplama test eder

### Gerçek Dünya Kullanımı:
```csharp
// Uygulama başlangıcında veya veri yenilemede oluşturulur
var campaigns = GetAllCampaigns();

// Seçenek 1: List
var list = new List<Campaign>();
foreach (var c in campaigns) list.Add(c);

// Seçenek 2: Satıcıya göre gruplandırılmış Dictionary
var dict = new Dictionary<int, List<Campaign>>();
foreach (var c in campaigns)
{
    if (!dict.ContainsKey(c.VendorCode))
        dict[c.VendorCode] = new List<Campaign>();
    dict[c.VendorCode].Add(c);
}

// Seçenek 3: LINQ GroupBy
var dictLinq = campaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());
```

### Neden Önemli?
- Bu dictionary'yi bir kez oluşturursunuz (uygulama başlangıcında veya cache yenilemede)
- Kapasite önceden ayırmak önemli ölçüde bellek tahsisini azaltabilir
- Doğru yapıyı seçmek sonraki tüm aramaları etkiler

### Test Edilen Yöntemler:

#### `BuildList()` ve `BuildList_WithCapacity()`
```csharp
// Kapasitesiz
var result = new List<CardCampaign>();

// Kapasiteli
var result = new List<CardCampaign>(_sourceCampaigns.Count);
```
- **Fark**: Kapasiteli versiyon dizi yeniden tahsisini önler
- **Performans Kazancı**: ~2-3x daha az allocation, %20-30 daha hızlı

#### `BuildHashSet()` ve `BuildHashSet_WithCapacity()`
```csharp
var result = new HashSet<CardCampaign>(_sourceCampaigns.Count);
```
- **Ne Zaman**: Benzersiz öğeler gerektiğinde
- **Avantaj**: Otomatik deduplication + hızlı lookup

#### `BuildDictionary()` ve `BuildDictionary_WithCapacity()`
```csharp
var result = new Dictionary<int, CardCampaign>(_sourceCampaigns.Count);
```
- **Ne Zaman**: Anahtar-değer çiftleri için
- **Avantaj**: O(1) anahtar ile erişim

#### `BuildDictionary_Grouped()` - Manuel Gruplama
```csharp
var result = new Dictionary<int, List<CardCampaign>>();
foreach (var campaign in _sourceCampaigns)
{
    if (!result.ContainsKey(campaign.VendorCode))
    {
        result[campaign.VendorCode] = new List<CardCampaign>();
    }
    result[campaign.VendorCode].Add(campaign);
}
```
- **Nasıl Çalışır**: Her satıcı için liste oluşturur, kampanyaları ekler
- **Avantaj**: Tam kontrol, genellikle en hızlı

#### `BuildDictionary_Grouped_LINQ()` - LINQ Gruplama
```csharp
return _sourceCampaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());
```
- **Nasıl Çalışır**: LINQ GroupBy kullanır
- **Avantaj**: Çok okunabilir, tek satır
- **Dezavantaj**: Manuel döngüden biraz daha yavaş

### Beklenen Sonuç:
- Kapasiteli = daha hızlı, daha az allocation
- Manuel döngü, büyük veri setleri için LINQ'dan biraz daha hızlı
- Ama LINQ daha okunabilir ve yine de yeterince hızlı

---

## 4. JoinOperationBenchmarks.cs - Birleştirme İşlemi Testleri

### Ne Yapar?
TAMAMEN tam operasyonu test eder: fiyatları kampanyalarıyla birleştirme. Bu **en önemli** benchmark çünkü yapmanız gereken asıl görevi test ediyor.

### Test Senaryosu:
- Bir fiyat listesi var
- Bir kampanya listesi (veya önceden gruplandırılmış dictionary) var
- Doğru kampanyaları her fiyata eklenmesi gerekiyor
- 5 farklı yaklaşımı karşılaştırır

### Test Edilen Yöntemler:

#### Yaklaşım 1: `ManualLoop_WithDictionary()` (Baseline)
```csharp
var result = new List<Price>(_prices.Count);
foreach (var price in _prices)
{
    var priceWithCampaigns = new Price { ... };

    if (_campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns))
    {
        priceWithCampaigns.CardCampaigns = new List<CardCampaign>(campaigns);
    }

    result.Add(priceWithCampaigns);
}
```
- **Artı**: Hızlı dictionary araması (O(1))
- **Eksi**: Biraz daha fazla kod
- **Ne Zaman**: %90 durumda varsayılan seçim

#### Yaklaşım 2: `ManualLoop_WithLinearSearch()`
```csharp
foreach (var price in _prices)
{
    var priceWithCampaigns = new Price { ... };

    var campaigns = _campaigns
        .Where(c => c.VendorCode == price.VendorCode)
        .ToList();

    priceWithCampaigns.CardCampaigns = campaigns;
    result.Add(priceWithCampaigns);
}
```
- **Artı**: Ön işlem gerekmez
- **Eksi**: YAVAŞ! O(n²) - her fiyat için TÜM kampanya listesini tarar
- **Sonuç**: ASLA KULLANMAYIN büyük veri setlerinde

#### Yaklaşım 3: `LINQ_Join()` - GroupJoin
```csharp
return _prices
    .GroupJoin(
        _campaigns,
        price => price.VendorCode,
        campaign => campaign.VendorCode,
        (price, campaigns) => new Price
        {
            ...
            CardCampaigns = campaigns.ToList()
        })
    .ToList();
```
- **Artı**: Temiz, deklaratif kod
- **Eksi**: Manuel döngüden biraz daha yavaş
- **Ne Zaman**: Kod temizliği önemliyse

#### Yaklaşım 4: `LINQ_Join_WithPreGroupedCampaigns()`
```csharp
return _prices
    .Select(price => new Price
    {
        ...
        CardCampaigns = _campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns)
            ? new List<CardCampaign>(campaigns)
            : new List<CardCampaign>()
    })
    .ToList();
```
- **Artı**: Hızlı + okunabilir
- **Eksi**: Manuel döngüden çok az daha yavaş
- **Ne Zaman**: LINQ tercih ediliyorsa ve dictionary önceden hazırsa

#### Yaklaşım 5: `LINQ_SelectMany_Flatten()`
```csharp
return _prices
    .SelectMany(
        price => _campaigns.Where(c => c.VendorCode == price.VendorCode).DefaultIfEmpty(),
        (price, campaign) => new { Price = price, Campaign = campaign })
    .GroupBy(x => x.Price.Id)
    .Select(g => new Price { ... })
    .ToList();
```
- **Artı**: Karmaşık senaryoları ele alır
- **Eksi**: Daha karmaşık, daha yavaş
- **Ne Zaman**: Özel edge case'ler varsa

### Neden Bu En Önemli Benchmark?
Bu benchmark şu soruyu cevaplıyor: **"Kampanyaları fiyatlara eşleştirmenin en hızlı yolu nedir?"**

### Beklenen Sonuç:
1. 🥇 Manuel loop + Dictionary: En hızlı
2. 🥈 LINQ + Önceden Gruplandırılmış Dict: Çok yakın, daha okunabilir
3. 🥉 LINQ GroupJoin: İyi, temiz kod
4. ❌ Linear search: YAVAŞ (kaçının!)
5. ❌ SelectMany: En yavaş ama özel durumları ele alır

---

## Benchmark'lar Nasıl Birlikte Çalışır?

Bu benchmark'lar birbirinin üzerine inşa edilir:

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. LookupBenchmarks                                             │
│    → Dictionary/HashSet'in aramalarda en iyi olduğunu kanıtlar │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. CollectionBuildingBenchmarks                                 │
│    → Dictionary'yi verimli şekilde nasıl oluşturacağını gösterir│
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. ParallelBenchmarks                                           │
│    → Paralel işlemin yardımcı olup olmadığını test eder         │
└─────────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. JoinOperationBenchmarks                                      │
│    → Gerçek görev için hepsini bir araya getirir                │
└─────────────────────────────────────────────────────────────────┘
```

---

## Tam Resim: Fiyat Karşılaştırma Uygulamanız İçin

```csharp
// BİR KEZ uygulama başlangıcında (CollectionBuildingBenchmarks bölgesi)
var campaignsByVendor = campaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());
// Bunu memory cache veya Redis'e kaydedin

// HER istek için (JoinOperationBenchmarks bölgesi)
var prices = GetPricesForProduct(productId);
var result = new List<Price>(prices.Count);

foreach (var price in prices)  // Sıralı, paralel değil (ParallelBenchmarks)
{
    var priceWithCampaigns = new Price { ... };

    // Dictionary araması (LookupBenchmarks)
    if (campaignsByVendor.TryGetValue(price.VendorCode, out var campaigns))
    {
        priceWithCampaigns.CardCampaigns = campaigns;
    }

    result.Add(priceWithCampaigns);
}
```

Her benchmark bu bulmacadaki bir parçayı doğrular!

---

## Özet Tablo: Hangi Benchmark Neyi Test Eder?

| Benchmark | Test Ettiği Şey | Asıl Soru | Karar |
|-----------|----------------|-----------|-------|
| **LookupBenchmarks** | Arama hızı | List mi, HashSet mi, Dictionary mi? | HashSet/Dictionary |
| **CollectionBuildingBenchmarks** | Koleksiyon oluşturma | Kapasite ayırmalı mıyım? LINQ mi manuel mi? | Kapasiteli + İkisi de iyi |
| **ParallelBenchmarks** | Paralel işleme | Parallel kullanmalı mıyım? | < 1K için hayır |
| **JoinOperationBenchmarks** | Tam eşleştirme | En hızlı birleştirme yöntemi nedir? | Manuel + Dictionary |

---

## Hızlı Karar Ağacı

```
Kampanyaları fiyatlara eşleştirmek istiyorum
    │
    ├─ Veri boyutum < 1000 öğe
    │   └─→ Manuel loop + Dictionary ✅
    │
    ├─ Veri boyutum > 10000 öğe + Ağır CPU işi
    │   └─→ PLINQ + Dictionary ✅
    │
    ├─ Kod temizliği çok önemli
    │   └─→ LINQ Select + Dictionary ✅
    │
    └─ Sadece varlık kontrolü yapacağım
        └─→ HashSet ✅
```

---

**Not**: Bu benchmark'ları çalıştırdıktan sonra, gerçek sayılarla `BENCHMARK_ANALIZI.md` dosyasını güncelleyin!
