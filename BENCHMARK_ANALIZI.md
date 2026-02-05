# Benchmark Analizi ve Öneriler

## Yönetici Özeti

Bu doküman, fiyat karşılaştırma uygulaması için performans analizi ve öneriler içermektedir. Özellikle satıcı kart kampanyalarının ürün fiyatlarına eşleştirilmesi senaryosuna odaklanmaktadır.

## Senaryo Genel Bakış

- **Amaç**: Kart kampanyalarını fiyatlara verimli şekilde eşleştirmek
- **Veri**: Fiyatlardaki satıcı kodları, satıcıya göre gruplandırılmış kampanyalar
- **Ölçek**: 100, 500 ve 1.000 öğe ile test edilmiştir

---

## Önerilen Yaklaşım

### **Kazanan: Dictionary Lookup ile Manuel Loop**

```csharp
// 1. Dictionary'yi bir kez oluşturun (veri yüklenirken)
var campaignsByVendor = campaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());

// 2. Her fiyat sorgusu/sayfa yüklemesinde kullanın
var result = new List<Price>(prices.Count); // Mutlaka kapasite belirtin!

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

### Neden Bu Kazanıyor:

✅ **O(1) arama karmaşıklığı** - Dictionary.TryGetValue sabit zamanda çalışır
✅ **Paralelleştirme maliyeti yok** - <1K öğe için sıralı işlem daha hızlı
✅ **Minimum bellek tahsisi** - Önceden ayrılmış kapasite yeniden boyutlandırmayı önler
✅ **Basit ve okunabilir** - Bakımı ve debug edilmesi kolay
✅ **Öngörülebilir performans** - Thread senkronizasyonu maliyeti yok

---

## Performans Hiyerarşisi (Beklenen)

### 1. Arama (Lookup) İşlemleri

| Yaklaşım | Karmaşıklık | En İyi Kullanım | Performans |
|----------|-------------|-----------------|------------|
| **Dictionary/HashSet** | O(1) | Her boyut | ⚡⚡⚡ **EN İYİ** |
| List.Contains | O(n) | < 50 öğe | ❌ Büyük listeler için kaçının |

**Önemli Bilgi**: Listeniz ~50-100 öğeyi geçtiğinde, Dictionary/HashSet dramatik şekilde daha hızlı hale gelir.

### 2. Birleştirme/Eşleştirme İşlemleri

| Yaklaşım | Hız Sırası | Bellek | Okunabilirlik | Ne Zaman Kullanılır |
|----------|-----------|---------|---------------|---------------------|
| **Manuel Loop + Dictionary** | 🥇 1. | Düşük | Yüksek | **Varsayılan seçim** |
| LINQ + Önceden Gruplandırılmış Dict | 🥈 2. | Düşük | Çok Yüksek | Temiz kod önceliği |
| LINQ GroupJoin | 🥉 3. | Orta | Yüksek | Tek seferlik işlemler |
| Parallel.ForEach | 4.* | Yüksek | Orta | Sadece 10K+ öğe |
| PLINQ | 4.* | Yüksek | Yüksek | Sadece 10K+ öğe |
| Linear Search (Where) | ❌ Son | Düşük | Yüksek | **ASLA kullanmayın** (O(n²)) |

*Paralel yaklaşımlar sadece büyük veri setleri (10K+) ve CPU-yoğun işlerde kazanır

### 3. Koleksiyon Oluşturma

| Yaklaşım | Performans | Ne Zaman Kullanılır |
|----------|-------------|---------------------|
| **List (kapasiteyle)** | ⚡⚡⚡ | **Her zaman** boyut biliniyorsa |
| List (kapasitesiz) | ⚡ | Boyut bilinmiyorsa |
| HashSet (kapasiteyle) | ⚡⚡⚡ | Benzersizlik gerekli + boyut biliniyor |
| HashSet (kapasitesiz) | ⚡ | Benzersizlik gerekli |
| Dictionary (kapasiteyle) | ⚡⚡⚡ | Anahtar-değer + boyut biliniyor |
| Dictionary (kapasitesiz) | ⚡ | Anahtar-değer çiftleri |

**Temel Kural**: Boyutu biliyorsanız mutlaka kapasiteyi önceden ayırın! Bu, dizi yeniden boyutlandırmayı ortadan kaldırır.

---

## Senaryoya Göre Detaylı Öneriler

### Senaryo 1: Ürün Sayfası Yükleme (100-500 fiyat)
**Öneri**: Manuel loop + Dictionary

```csharp
// Bu dictionary'yi bellekte veya Redis'te önbelleğe alın
var campaignsByVendor = GetCampaignsDictionary();

var prices = GetPricesForProduct(productId);
var result = new List<Price>(prices.Count);

foreach (var price in prices)
{
    // ... TryGetValue ile manuel loop
}
```

**Neden**: Bu boyut için en hızlı, paralelleştirme maliyeti yok, basit kod.

---

### Senaryo 2: Toplu İşlem (1000+ fiyat)
**Öneri**: Önceden gruplandırılmış dictionary ile PLINQ'yu düşünün

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

**Neden**: Paralel işleme büyük partilerde yardımcı olur, PLINQ, Parallel.ForEach'ten daha temizdir.

---

### Senaryo 3: API Response (Gerçek zamanlı sorgu)
**Öneri**: LINQ + Önceden Gruplandırılmış Dictionary (okunabilirlik için)

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

**Neden**: Manuel loop'tan biraz daha yavaş ama çok daha okunabilir, API kodu için iyi denge.

---

## Kaçınılması Gereken Anti-Pattern'ler

### ❌ YAPMAYIN: Loop İçinde Linear Search (O(n²))
```csharp
// Bu ÇOK YAVAŞ - O(n²) karmaşıklık
foreach (var price in prices)
{
    var campaigns = allCampaigns
        .Where(c => c.VendorCode == price.VendorCode)
        .ToList(); // KÖTÜ!
}
```

### ❌ YAPMAYIN: Kapasiteyi Önceden Ayarlamayı Unutmak
```csharp
var result = new List<Price>(); // Birden fazla kez yeniden boyutlanacak - YAVAŞ
```

### ❌ YAPMAYIN: Küçük Veri Setleri için Parallel Kullanmak
```csharp
// 100 öğe için paralel maliyet > gerçek iş
Parallel.ForEach(100items, ...); // foreach'ten DAHA YAVAŞ
```

### ❌ YAPMAYIN: Her Seferinde Dictionary'yi Yeniden Oluşturmak
```csharp
// Her istekte dictionary'yi yeniden oluşturmak - İSRAF
var dict = campaigns.GroupBy(...).ToDictionary(...); // Bir kez yapın, önbelleğe alın!
```

---

## En İyi Uygulamalar Özeti

### 1. **Aramalar için Dictionary Kullanın**
- Kampanyaları satıcı koduna göre bir kez Dictionary'de gruplayın
- Bu dictionary'yi birden fazla istekte yeniden kullanın
- Yüksek trafikli uygulamalar için Redis/Memory cache düşünün

### 2. **Her Zaman Kapasiteyi Önceden Ayırın**
```csharp
var result = new List<Price>(prices.Count); // İyi!
var result = new List<Price>(); // Kötü - yeniden boyutlanacak
```

### 3. **< 1K Öğe için Basit Tutun**
- Sıralı loop'lar yeterince hızlı
- Paralelleştirme maliyetinden kaçının
- Okunabilirlik için optimize edin

### 4. **10K+ Öğe için Paralelleştirmeyi Düşünün**
- PLINQ kullanın (Parallel.ForEach'ten daha temiz)
- Paralellik derecesini açıkça belirleyin
- Gerçek veri boyutlarınızla test edin

### 5. **Gerçek Veri ile Profil Çıkarın**
- Bu benchmark'lar sentetik veri kullanır
- Gerçek dünya veri desenleri önemlidir
- Üretime benzer ortamda ölçüm yapın

---

## Uygulama Kontrol Listesi

- [ ] Uygulama başlangıcında kampanya dictionary'sini oluştur
- [ ] Dictionary'yi bellekte önbelleğe al (dağıtık sistemler için Redis düşün)
- [ ] Standart sorgular için manuel loop + Dictionary.TryGetValue kullan
- [ ] Boyut bilindiğinde List kapasitesini önceden ayır
- [ ] Büyük listeler (> 50 öğe) için List.Contains kullanma
- [ ] Sadece toplu işlemler için paralel işleme kullan (10K+ öğe)
- [ ] Üretimde gerçek performansı izle
- [ ] Çok büyük sonuç setleri için sayfalama düşün

---

## Bu Kararları Ne Zaman Yeniden Gözden Geçirmeli

1. **Veri boyutu önemli ölçüde artarsa** (>10K fiyat/sorgu)
   - Paralel yaklaşımlarla yeniden test edin
   - Sayfalama/streaming düşünün

2. **Karmaşık iş mantığı eklenir** (öğe başına ağır CPU işi)
   - Paralelleştirme faydaları artar
   - Benchmark'ları yeniden çalıştırın

3. **Bellek sorun olursa**
   - Streaming/yield return düşünün
   - Dictionary önbellekleme stratejisini değerlendirin

4. **Üretimde performans sorunları**
   - Gerçek veri ile profil çıkarın
   - Önce veritabanı sorgu performansını kontrol edin
   - VendorCode'a veritabanı index'i eklemeyi düşünün

---

## Hızlı Referans

**Bu uygulamadaki kullanım durumlarının %99'u için:**
```csharp
// Başlangıçta bir kez
var campaignsByVendor = campaigns
    .GroupBy(c => c.VendorCode)
    .ToDictionary(g => g.Key, g => g.ToList());

// Her istek için
var result = new List<Price>(prices.Count);
foreach (var price in prices)
{
    // ... TryGetValue kullanarak kampanyalarla fiyat oluştur
}
```

**Bu basit, hızlı ve bakımı kolay. Karmaşıklaştırmayın!**

---

## Benchmark Sonuçları

### Lookup Benchmarks (Arama Testleri)

**Donanım**: Intel Core i5-12400F (6 çekirdek, 12 thread), .NET 8.0

| Yöntem | 100 Öğe | 500 Öğe | 1000 Öğe | Kazanan |
|--------|---------|---------|----------|---------|
| **HashSet** | 272 ns | 239 ns | 263 ns | 🥇 **EN HIZLI** |
| **Dictionary** | 287 ns | 276 ns | 296 ns | 🥈 İkinci |
| **List** | 586 ns | 1,951 ns | 4,256 ns | 🐌 Boyutla kötüleşiyor |

#### Temel Çıkarımlar:

1. **HashSet açık ara kazanan** - Tüm boyutlarda tutarlı şekilde en hızlı
2. **Dictionary çok yakın** - HashSet'ten sadece ~10-30ns daha yavaş
3. **List performansı dramatik şekilde düşüyor**:
   - 100 öğede: 2x daha yavaş
   - 500 öğede: 8x daha yavaş
   - 1000 öğede: 16x daha yavaş (O(n) karmaşıklığı görülüyor)

4. **Sıfır bellek tahsisi** - Tüm yaklaşımlar aramalar için ekstra bellek ayırmıyor

#### Öneri Doğrulandı:
✅ Satıcı kodu aramaları için **HashSet** veya **Dictionary** kullanın. Aralarındaki fark ihmal edilebilir düzeyde (~%3-10), bu yüzden ilişkili veriye ihtiyacınıza göre seçin:
- **HashSet**: Sadece varlık kontrolü gerekiyorsa
- **Dictionary**: İlişkili veri almanız gerekiyorsa (kampanya listeleri gibi)

---

### Parallel Benchmarks (Paralel İşlem Testleri)

*Benchmark tamamlandığında sonuçlar buraya eklenecek*

---

### Collection Building Benchmarks (Koleksiyon Oluşturma Testleri)

*Benchmark tamamlandığında sonuçlar buraya eklenecek*

---

### Join Operation Benchmarks (Birleştirme İşlemi Testleri)

*Benchmark tamamlandığında sonuçlar buraya eklenecek*

---

**Doküman Versiyonu**: 1.0
**Son Güncelleme**: 2026-02-05
**Test Konfigürasyonu**: .NET 8.0, BenchmarkDotNet, Boyutlar: 100/500/1000
