namespace PerformanceBenchmarks.Models;

public class Price
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int VendorCode { get; set; }
    public decimal Amount { get; set; }
    public List<CardCampaign> CardCampaigns { get; set; } = new();
}
