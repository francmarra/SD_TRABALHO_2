using Shared.MongoDB;
using Shared.Models;

namespace ConfigImporter
{
    public class TestConfigService
    {
        public static async Task TestDatabaseConnectivity()
        {
            try
            {
                var configService = new ConfigService();
                
                Console.WriteLine("Testing ConfigService database connectivity...\n");
                
                // Test aggregator configs
                Console.WriteLine("=== AGGREGATOR CONFIGURATIONS ===");
                var agrConfigs = await configService.GetAllAgrConfigsAsync();
                Console.WriteLine($"Found {agrConfigs.Count} aggregator configurations:");
                foreach (var config in agrConfigs)
                {
                    Console.WriteLine($"  ID: {config.Id}, Port: {config.Port}");
                }
                
                // Test wavy configs
                Console.WriteLine("\n=== WAVY CONFIGURATIONS ===");
                var wavyConfigs = await configService.GetAllWavyConfigsAsync();
                Console.WriteLine($"Found {wavyConfigs.Count} wavy configurations:");
                foreach (var config in wavyConfigs)
                {
                    Console.WriteLine($"  WAVY_ID: {config.WavyId}, Status: {config.Status}, Last Sync: {config.LastSync}");
                }
                  // Test specific lookups
                Console.WriteLine("\n=== SPECIFIC TESTS ===");
                
                var northConfig = await configService.GetAgrConfigAsync("N_Agr");
                if (northConfig != null)
                {
                    Console.WriteLine($"North aggregator config found: Port {northConfig.Port}");
                }
                else
                {
                    Console.WriteLine("North aggregator config not found");
                }
                
                var wavyConfig = await configService.GetWavyConfigAsync("N_Wavy01");
                if (wavyConfig != null)
                {
                    Console.WriteLine($"N_Wavy01 config found: Status {wavyConfig.Status}");
                }
                else
                {
                    Console.WriteLine("N_Wavy01 config not found");
                }
                
                Console.WriteLine("\nDatabase connectivity test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing database connectivity: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
