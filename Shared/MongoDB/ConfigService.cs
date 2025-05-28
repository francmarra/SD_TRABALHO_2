using MongoDB.Driver;
using Shared.Models;
using Shared.MongoDB;

namespace Shared.MongoDB
{
    public class ConfigService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<ConfigAgr> _configAgrCollection;
        private readonly IMongoCollection<ConfigWavy> _configWavyCollection;

        public ConfigService()
        {
            var client = new MongoClient(MongoDBConfig.CONNECTION_STRING);
            _database = client.GetDatabase(MongoDBConfig.DATABASE_NAME);
            _configAgrCollection = _database.GetCollection<ConfigAgr>(MongoDBConfig.CONFIG_AGR_COLLECTION);
            _configWavyCollection = _database.GetCollection<ConfigWavy>(MongoDBConfig.CONFIG_WAVY_COLLECTION);
        }

        // Get aggregator configuration by ID
        public async Task<ConfigAgr?> GetAgrConfigAsync(string agrId)
        {
            return await _configAgrCollection.Find(x => x.AgrId == agrId).FirstOrDefaultAsync();
        }

        // Get all aggregator configurations
        public async Task<List<ConfigAgr>> GetAllAgrConfigsAsync()
        {
            return await _configAgrCollection.Find(_ => true).ToListAsync();
        }

        // Get wavy configuration by ID
        public async Task<ConfigWavy?> GetWavyConfigAsync(string wavyId)
        {
            return await _configWavyCollection.Find(x => x.WavyId == wavyId).FirstOrDefaultAsync();
        }

        // Get all wavy configurations
        public async Task<List<ConfigWavy>> GetAllWavyConfigsAsync()
        {
            return await _configWavyCollection.Find(_ => true).ToListAsync();
        }

        // Update wavy status
        public async Task UpdateWavyStatusAsync(string wavyId, int status, DateTime lastSync)
        {
            var filter = Builders<ConfigWavy>.Filter.Eq(x => x.WavyId, wavyId);
            var update = Builders<ConfigWavy>.Update
                .Set(x => x.Status, status)
                .Set(x => x.LastSync, lastSync);
            
            await _configWavyCollection.UpdateOneAsync(filter, update);
        }

        // Insert aggregator config
        public async Task InsertAgrConfigAsync(ConfigAgr config)
        {
            await _configAgrCollection.InsertOneAsync(config);
        }

        // Insert wavy config
        public async Task InsertWavyConfigAsync(ConfigWavy config)
        {
            await _configWavyCollection.InsertOneAsync(config);
        }

        // Clear and insert all aggregator configs
        public async Task ReplaceAllAgrConfigsAsync(List<ConfigAgr> configs)
        {
            await _configAgrCollection.DeleteManyAsync(_ => true);
            if (configs.Any())
            {
                await _configAgrCollection.InsertManyAsync(configs);
            }
        }

        // Clear and insert all wavy configs
        public async Task ReplaceAllWavyConfigsAsync(List<ConfigWavy> configs)
        {
            await _configWavyCollection.DeleteManyAsync(_ => true);
            if (configs.Any())
            {
                await _configWavyCollection.InsertManyAsync(configs);
            }
        }
    }
}
