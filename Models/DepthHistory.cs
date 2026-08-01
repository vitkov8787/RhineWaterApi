namespace RhineWaterApi.Models;

public class DepthHistory
{
    public int Id { get; set; }

    public string StationName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public int MinWaterLevelCm { get; set; }

    public int MinChannelDepthCm { get; set; }

    public int MinRecommendedDraftCm { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}