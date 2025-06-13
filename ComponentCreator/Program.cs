using System.Text.Json;
using Shared.Models;
using Shared.MongoDB;

namespace ComponentCreator
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ComponentCreator.exe <type> <json_data>");
                Console.WriteLine("Types: aggregator, wavy");
                return 1;
            }

            string componentType = args[0].ToLower();
            string jsonData = args[1];

            try
            {
                var configService = new ConfigService();

                switch (componentType)
                {
                    case "aggregator":
                        await CreateAggregator(configService, jsonData);
                        break;
                    case "wavy":
                        await CreateWavy(configService, jsonData);
                        break;
                    default:
                        Console.WriteLine($"Unknown component type: {componentType}");
                        return 1;
                }

                Console.WriteLine($"SUCCESS: {componentType} created in MongoDB");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
        }

        static async Task CreateAggregator(ConfigService configService, string jsonData)
        {
            var formData = JsonSerializer.Deserialize<AggregatorFormData>(jsonData);
            if (formData == null)
            {
                throw new ArgumentException("Invalid JSON data for aggregator");
            }

            // Map region to continent code
            string continentCode = MapRegionToContinentCode(formData.region);

            var configAgr = new ConfigAgr
            {
                AgrId = formData.id,
                Continent = formData.region,
                ContinentCode = continentCode,
                ServerId = $"{continentCode}-S",
                QueueName = $"{formData.id}_queue",
                IsActive = true,
                Latitude = formData.latitude,
                Longitude = formData.longitude,
                Ocean = formData.ocean,
                AreaType = formData.areaType,
                SubscribedDataTypes = formData.dataTypes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await configService.InsertAgrConfigAsync(configAgr);
        }

        static async Task CreateWavy(ConfigService configService, string jsonData)
        {
            var formData = JsonSerializer.Deserialize<WavyFormData>(jsonData);
            if (formData == null)
            {
                throw new ArgumentException("Invalid JSON data for wavy");
            }

            var configWavy = new ConfigWavy
            {
                WavyId = formData.id,
                Status = formData.status,
                LastSync = DateTime.UtcNow,
                DataInterval = formData.dataInterval,
                IsActive = formData.status == 1,
                Latitude = formData.latitude,
                Longitude = formData.longitude,
                Ocean = formData.ocean,
                AreaType = formData.areaType,
                RegionCoverage = formData.regionCoverage,
                CreatedAt = DateTime.UtcNow
            };

            await configService.InsertWavyConfigAsync(configWavy);
        }

        static string MapRegionToContinentCode(string region)
        {
            return region switch
            {
                "North America" => "NA",
                "South America" => "SA",
                "Europe" => "EU",
                "Africa" => "AF",
                "Asia" => "AS",
                "Oceania" => "OC",
                "Antarctica" => "AQ",
                _ => "XX" // Unknown
            };
        }
    }

    // Data models for JSON deserialization
    public class AggregatorFormData
    {
        public string id { get; set; } = "";
        public string region { get; set; } = "";
        public string ocean { get; set; } = "";
        public string areaType { get; set; } = "";
        public double latitude { get; set; }
        public double longitude { get; set; }
        public List<string> dataTypes { get; set; } = new();
    }

    public class WavyFormData
    {
        public string id { get; set; } = "";
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string ocean { get; set; } = "";
        public string areaType { get; set; } = "";
        public string regionCoverage { get; set; } = "";
        public int dataInterval { get; set; }
        public int status { get; set; }
    }
}
