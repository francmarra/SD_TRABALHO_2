using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class WavyMessage
    {
        [JsonPropertyName("wavy_id")]
        public string WavyId { get; set; } = "";
        
        [JsonPropertyName("continent")]
        public string Continent { get; set; } = "";
        
        [JsonPropertyName("continent_code")]
        public string ContinentCode { get; set; } = "";
        
        [JsonPropertyName("aggregator_id")]
        public string AggregatorId { get; set; } = "";
        
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; } = "";
        
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";
        
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
        
        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }
        
        [JsonPropertyName("co2")]
        public double Co2 { get; set; }
        
        // Legacy support for sensor array (will be deprecated)
        [JsonPropertyName("sensors")]
        public SensorData[]? Sensors { get; set; }
        
        // Legacy support
        [JsonPropertyName("agregador_id")]
        public string AgregadorId { get; set; } = "";
    }

    public class SensorData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
}
