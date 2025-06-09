using System.Globalization;
using Shared.Models;
using Shared.MongoDB;

namespace ConfigImporter
{
    class Program
    {        static async Task Main(string[] args)
        {
            // Check if user wants to test database connectivity
            if (args.Length > 0 && args[0] == "--test")
            {
                await TestConfigService.TestDatabaseConnectivity();
                return;
            }            // Check if user wants to test coordinate configuration
            if (args.Length > 0 && args[0] == "--test-coords")
            {
                CoordinateConfigTest.TestCoordinateConfiguration();
                return;
            }

            Console.WriteLine("=== Continent-Based Configuration Importer ===");
            Console.WriteLine("This will replace the current configuration with continent-based system.");
            Console.WriteLine();

            // Ask user which configuration to import
            Console.WriteLine("Select configuration to import:");
            Console.WriteLine("1. Legacy (Cardinal Points: N, S, E, W)");
            Console.WriteLine("2. Continents (EU, NA, SA, AF, AS, OC, AQ)");
            Console.WriteLine("3. Both (Legacy first, then Continents)");
            Console.Write("Enter choice (1-3): ");
            
            var choice = Console.ReadLine();
            
            var configService = new ConfigService();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ImportLegacyConfigs(configService);
                        break;
                    case "2":
                        await ImportContinentConfigs(configService);
                        break;
                    case "3":
                        await ImportLegacyConfigs(configService);
                        Console.WriteLine("\nNow importing continent configs...");
                        await ImportContinentConfigs(configService);
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Importing continent configs by default...");
                        await ImportContinentConfigs(configService);
                        break;
                }

                Console.WriteLine("\n✅ Config import completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during import: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static async Task ImportLegacyConfigs(ConfigService configService)
        {
            Console.WriteLine("\n🔄 Importing LEGACY configuration (Cardinal Points)...");
            
            // Import legacy aggregator configs
            await ImportLegacyAgrConfigs(configService);
            
            // Import legacy wavy configs
            await ImportLegacyWavyConfigs(configService);
        }

        static async Task ImportContinentConfigs(ConfigService configService)
        {
            Console.WriteLine("\n🌍 Importing CONTINENT-BASED configuration...");
            
            // Import server configs first
            await ImportServerConfigs(configService);
            
            // Import aggregator configs
            await ImportContinentAgrConfigs(configService);
            
            // Import wavy configs
            await ImportContinentWavyConfigs(configService);
        }

        static async Task ImportLegacyAgrConfigs(ConfigService configService)
        {
            Console.WriteLine("  📡 Importing legacy aggregator configs...");
            
            var configs = new List<ConfigAgr>();
            var filePath = "../Config/config_agr.csv";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  ⚠️  Legacy file not found: {filePath}");
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2)
                {
                    var agrId = parts[0].Trim();
                    var regionCode = agrId.Split('_')[0]; // Extract N, S, E, W
                    
                    configs.Add(new ConfigAgr
                    {
                        AgrId = agrId,
                        Continent = GetLegacyRegionName(regionCode),
                        ContinentCode = regionCode,
                        ServerId = $"{regionCode}_Server",
                        Port = int.Parse(parts[1].Trim()),
                        QueueName = $"{regionCode.ToLower()}_aggregator_queue",
                        IsActive = true
                    });
                }
            }

            await configService.ReplaceAllAgrConfigsAsync(configs);
            Console.WriteLine($"  ✅ Imported {configs.Count} legacy aggregator configs");
        }

        static async Task ImportLegacyWavyConfigs(ConfigService configService)
        {
            Console.WriteLine("  📊 Importing legacy wavy configs...");
            
            var configs = new List<ConfigWavy>();
            var filePath = "../Config/config_wavy.csv";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  ⚠️  Legacy file not found: {filePath}");
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 3)
                {
                    var wavyId = parts[0].Trim();
                    var regionCode = wavyId.Split('_')[0]; // Extract N, S, E, W
                    
                    configs.Add(new ConfigWavy
                    {
                        WavyId = wavyId,
                        Continent = GetLegacyRegionName(regionCode),
                        ContinentCode = regionCode,
                        AggregatorId = $"{regionCode}_Agr",
                        ServerId = $"{regionCode}_Server",
                        Status = int.Parse(parts[1].Trim()),
                        LastSync = DateTime.Parse(parts[2].Trim(), null, DateTimeStyles.RoundtripKind),
                        DataInterval = 5000,
                        IsActive = true
                    });
                }
            }

            await configService.ReplaceAllWavyConfigsAsync(configs);
            Console.WriteLine($"  ✅ Imported {configs.Count} legacy wavy configs");
        }

        static async Task ImportServerConfigs(ConfigService configService)
        {
            Console.WriteLine("  🖥️  Importing server configs...");
            
            var configs = new List<ConfigServer>();
            var filePath = "../Config/config_server_continents.csv";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  ❌ File not found: {filePath}");
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
              // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 10)
                {
                    configs.Add(new ConfigServer
                    {
                        ServerId = parts[0].Trim(),
                        Continent = parts[1].Trim(),
                        ContinentCode = parts[2].Trim(),
                        Port = int.Parse(parts[3].Trim()),
                        QueueName = parts[4].Trim(),
                        DatabaseName = parts[5].Trim(),
                        IsActive = bool.Parse(parts[6].Trim()),
                        MaxConnections = int.Parse(parts[7].Trim()),
                        Latitude = double.Parse(parts[8].Trim(), CultureInfo.InvariantCulture),
                        Longitude = double.Parse(parts[9].Trim(), CultureInfo.InvariantCulture),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await configService.ReplaceAllServerConfigsAsync(configs);
            Console.WriteLine($"  ✅ Imported {configs.Count} server configs");
        }

        static async Task ImportContinentAgrConfigs(ConfigService configService)
        {
            Console.WriteLine("  📡 Importing continent aggregator configs...");
            
            var configs = new List<ConfigAgr>();
            var filePath = "../Config/config_agr_continents.csv";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  ❌ File not found: {filePath}");
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
              // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 9)
                {
                    configs.Add(new ConfigAgr
                    {
                        AgrId = parts[0].Trim(),
                        Continent = parts[1].Trim(),
                        ContinentCode = parts[2].Trim(),
                        ServerId = parts[3].Trim(),
                        Port = int.Parse(parts[4].Trim()),
                        QueueName = parts[5].Trim(),
                        IsActive = bool.Parse(parts[6].Trim()),
                        Latitude = double.Parse(parts[7].Trim(), CultureInfo.InvariantCulture),
                        Longitude = double.Parse(parts[8].Trim(), CultureInfo.InvariantCulture),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await configService.ReplaceAllAgrConfigsAsync(configs);
            Console.WriteLine($"  ✅ Imported {configs.Count} continent aggregator configs");
        }

        static async Task ImportContinentWavyConfigs(ConfigService configService)
        {
            Console.WriteLine("  📊 Importing continent wavy configs...");
            
            var configs = new List<ConfigWavy>();
            var filePath = "../Config/config_wavy_continents.csv";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"  ❌ File not found: {filePath}");
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {                var parts = lines[i].Split(',');
                if (parts.Length >= 11)
                {
                    configs.Add(new ConfigWavy
                    {
                        WavyId = parts[0].Trim(),
                        Continent = parts[1].Trim(),
                        ContinentCode = parts[2].Trim(),
                        AggregatorId = parts[3].Trim(),
                        ServerId = parts[4].Trim(),
                        Status = int.Parse(parts[5].Trim()),
                        LastSync = DateTime.Parse(parts[6].Trim(), null, DateTimeStyles.RoundtripKind),
                        DataInterval = int.Parse(parts[7].Trim()),
                        IsActive = bool.Parse(parts[8].Trim()),
                        Latitude = double.Parse(parts[9].Trim(), CultureInfo.InvariantCulture),
                        Longitude = double.Parse(parts[10].Trim(), CultureInfo.InvariantCulture),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await configService.ReplaceAllWavyConfigsAsync(configs);
            Console.WriteLine($"  ✅ Imported {configs.Count} continent wavy configs");
        }

        private static string GetLegacyRegionName(string regionCode)
        {
            return regionCode switch
            {
                "N" => "North Region",
                "S" => "South Region",
                "E" => "East Region",
                "W" => "West Region",
                _ => "Unknown Region"
            };
        }
    }
}
