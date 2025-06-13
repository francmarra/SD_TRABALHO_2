using MongoDB.Driver;
using Shared.Models;
using Shared.MongoDB;

class QuickCheck
{
    static async Task Main(string[] args)
    {
        var service = new ConfigService();
        
        Console.WriteLine("🔍 Checking ConfigAgr collection...");
        
        // Get all configs to see what's available
        var allConfigs = await service.GetAllAgrConfigsAsync();
        Console.WriteLine($"📊 Found {allConfigs.Count} configurations:");
        
        foreach (var config in allConfigs)
        {
            Console.WriteLine($"   • ID: '{config.AgrId}' | Continent: {config.Continent} ({config.ContinentCode})");
            Console.WriteLine($"     Ocean: {config.Ocean} | Area: {config.AreaType}");
            Console.WriteLine($"     Data Types: [{string.Join(", ", config.SubscribedDataTypes)}]");
            Console.WriteLine();
        }
        
        Console.WriteLine("\n🎯 Trying to get EU-Agr01 specifically...");
        var euAgr01 = await service.GetAgrConfigAsync("EU-Agr01");
        if (euAgr01 != null)
        {
            Console.WriteLine($"✅ Found EU-Agr01: {euAgr01.Continent} - {euAgr01.Ocean}");
        }
        else
        {
            Console.WriteLine("❌ EU-Agr01 not found!");
        }
    }
}
