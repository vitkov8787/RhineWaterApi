namespace RhineWaterApi.Models;

public class StationConfig
{
    public int Id { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int GlwCm { get; set; }
    public int GuaranteedDepthCm { get; set; }
    public int SafetyMarginCm { get; set; } = 30; // Твоят запас
    
    public bool IsMainStation { get; set; }
}