namespace PerformanceBenchmarks.Models;

public class CardCampaign
{
    public int Id { get; set; }
    public int VendorCode { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}
