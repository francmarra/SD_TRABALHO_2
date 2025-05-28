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
            }

            Console.WriteLine("Starting config import to MongoDB...");

            var configService = new ConfigService();

            try
            {
                // Import aggregator configs
                await ImportAgrConfigs(configService);
                
                // Import wavy configs
                await ImportWavyConfigs(configService);

                Console.WriteLine("Config import completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during import: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static async Task ImportAgrConfigs(ConfigService configService)
        {
            Console.WriteLine("Importing aggregator configs...");
            
            var configs = new List<ConfigAgr>();
            var lines = await File.ReadAllLinesAsync("../Config/config_agr.csv");
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2)
                {
                    configs.Add(new ConfigAgr
                    {
                        AgrId = parts[0].Trim(),
                        Port = int.Parse(parts[1].Trim())
                    });
                }
            }

            await configService.ReplaceAllAgrConfigsAsync(configs);
            Console.WriteLine($"Imported {configs.Count} aggregator configs");
        }

        static async Task ImportWavyConfigs(ConfigService configService)
        {
            Console.WriteLine("Importing wavy configs...");
            
            var configs = new List<ConfigWavy>();
            var lines = await File.ReadAllLinesAsync("../Config/config_wavy.csv");
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 3)
                {
                    configs.Add(new ConfigWavy
                    {
                        WavyId = parts[0].Trim(),
                        Status = int.Parse(parts[1].Trim()),
                        LastSync = DateTime.Parse(parts[2].Trim(), null, DateTimeStyles.RoundtripKind)
                    });
                }
            }

            await configService.ReplaceAllWavyConfigsAsync(configs);
            Console.WriteLine($"Imported {configs.Count} wavy configs");
        }
    }
}
