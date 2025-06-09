using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Shared.Models;

namespace ConfigImporter
{
    public class CoordinateConfigTest
    {
        private static readonly string ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Config");

        public static void TestCoordinateConfiguration()
        {
            Console.WriteLine("=== Coordinate Configuration Test ===");
            Console.WriteLine();

            // Test Server Configurations
            TestServerConfigurations();
            Console.WriteLine();

            // Test Aggregator Configurations
            TestAggregatorConfigurations();
            Console.WriteLine();

            // Test Wavy Configurations
            TestWavyConfigurations();
            Console.WriteLine();

            Console.WriteLine("=== Test Complete ===");
        }

        private static void TestServerConfigurations()
        {
            Console.WriteLine("📡 Server Configurations:");
            try
            {
                var filePath = Path.Combine(ConfigPath, "config_server_continents.csv");
                var servers = LoadServerConfigurations(filePath);
                
                Console.WriteLine($"Loaded {servers.Count} server configurations:");
                foreach (var server in servers)
                {
                    Console.WriteLine($"  • {server.ServerId} ({server.Continent}): [{server.Latitude:F4}, {server.Longitude:F4}]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading server configurations: {ex.Message}");
            }
        }

        private static void TestAggregatorConfigurations()
        {
            Console.WriteLine("🔄 Aggregator Configurations:");
            try
            {
                var filePath = Path.Combine(ConfigPath, "config_agr_continents.csv");
                var aggregators = LoadAggregatorConfigurations(filePath);
                
                Console.WriteLine($"Loaded {aggregators.Count} aggregator configurations:");
                foreach (var agr in aggregators)
                {
                    Console.WriteLine($"  • {agr.AgrId} ({agr.Continent}): [{agr.Latitude:F4}, {agr.Longitude:F4}]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading aggregator configurations: {ex.Message}");
            }
        }

        private static void TestWavyConfigurations()
        {
            Console.WriteLine("📊 Wavy Configurations:");
            try
            {
                var filePath = Path.Combine(ConfigPath, "config_wavy_continents.csv");
                var wavys = LoadWavyConfigurations(filePath);
                
                Console.WriteLine($"Loaded {wavys.Count} wavy configurations:");
                foreach (var wavy in wavys)
                {
                    Console.WriteLine($"  • {wavy.WavyId} ({wavy.Continent}): [{wavy.Latitude:F4}, {wavy.Longitude:F4}]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading wavy configurations: {ex.Message}");
            }
        }

        private static List<ConfigServer> LoadServerConfigurations(string filePath)
        {
            var servers = new List<ConfigServer>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2) return servers;

            var headers = lines[0].Split(',');
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length >= headers.Length)
                {
                    var server = new ConfigServer
                    {
                        ServerId = values[0].Trim(),
                        Continent = values[1].Trim(),
                        ContinentCode = values[2].Trim(),
                        Port = int.Parse(values[3].Trim()),
                        QueueName = values[4].Trim(),
                        DatabaseName = values[5].Trim(),
                        IsActive = bool.Parse(values[6].Trim()),
                        MaxConnections = int.Parse(values[7].Trim()),
                        Latitude = double.Parse(values[8].Trim(), CultureInfo.InvariantCulture),
                        Longitude = double.Parse(values[9].Trim(), CultureInfo.InvariantCulture)
                    };
                    servers.Add(server);
                }
            }
            
            return servers;
        }

        private static List<ConfigAgr> LoadAggregatorConfigurations(string filePath)
        {
            var aggregators = new List<ConfigAgr>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2) return aggregators;

            var headers = lines[0].Split(',');
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length >= headers.Length)
                {
                    var agr = new ConfigAgr
                    {
                        AgrId = values[0].Trim(),
                        Continent = values[1].Trim(),
                        ContinentCode = values[2].Trim(),
                        ServerId = values[3].Trim(),
                        Port = int.Parse(values[4].Trim()),
                        QueueName = values[5].Trim(),
                        IsActive = bool.Parse(values[6].Trim()),
                        Latitude = double.Parse(values[7].Trim(), CultureInfo.InvariantCulture),
                        Longitude = double.Parse(values[8].Trim(), CultureInfo.InvariantCulture)
                    };
                    aggregators.Add(agr);
                }
            }
            
            return aggregators;
        }

        private static List<ConfigWavy> LoadWavyConfigurations(string filePath)
        {
            var wavys = new List<ConfigWavy>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2) return wavys;

            var headers = lines[0].Split(',');
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length >= headers.Length)
                {
                    var wavy = new ConfigWavy
                    {
                        WavyId = values[0].Trim(),
                        Continent = values[1].Trim(),
                        ContinentCode = values[2].Trim(),
                        AggregatorId = values[3].Trim(),
                        ServerId = values[4].Trim(),
                        Status = int.Parse(values[5].Trim()),
                        LastSync = DateTime.Parse(values[6].Trim()),
                        DataInterval = int.Parse(values[7].Trim()),
                        IsActive = bool.Parse(values[8].Trim()),
                        Latitude = double.Parse(values[9].Trim(), CultureInfo.InvariantCulture),
                        Longitude = double.Parse(values[10].Trim(), CultureInfo.InvariantCulture)
                    };
                    wavys.Add(wavy);
                }
            }
            
            return wavys;
        }
    }
}
