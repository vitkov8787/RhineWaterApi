namespace RhineWaterApi.Models;

public class RhineWaterLevel
{
    public int Id { get; set; }
    public string StationName { get; set; } = string.Empty;
    public double Kilometer { get; set; }
    public int LevelCm { get; set; }
    public DateTime MeasuredAt { get; set; } 
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}