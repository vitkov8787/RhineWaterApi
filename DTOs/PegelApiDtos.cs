using System.Text.Json.Serialization;

namespace RhineWaterApi.DTOs;

public class PegelStationDto {
    [JsonPropertyName("shortname")] public string Name { get; set; } = "";
    [JsonPropertyName("km")] public double Km { get; set; }
    [JsonPropertyName("timeseries")] public List<PegelTimeseriesDto>? Timeseries { get; set; }
}

public class PegelTimeseriesDto {
    [JsonPropertyName("shortname")] public string Shortname { get; set; } = "";
    [JsonPropertyName("currentMeasurement")] public PegelMeasurementDto? CurrentMeasurement { get; set; }
}

public class PegelMeasurementDto {
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
    [JsonPropertyName("value")] public double Value { get; set; }
}