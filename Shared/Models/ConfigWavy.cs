using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Models
{
    public class ConfigWavy
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("WAVY_ID")]
        public string WavyId { get; set; } = string.Empty;

        [BsonElement("continent")]
        public string Continent { get; set; } = string.Empty;

        [BsonElement("continent_code")]
        public string ContinentCode { get; set; } = string.Empty;

        [BsonElement("aggregator_id")]
        public string AggregatorId { get; set; } = string.Empty;

        [BsonElement("server_id")]
        public string ServerId { get; set; } = string.Empty;

        [BsonElement("status")]
        public int Status { get; set; }

        [BsonElement("last_sync")]
        public DateTime LastSync { get; set; }

        [BsonElement("data_interval")]
        public int DataInterval { get; set; } = 5000; // Default 5 seconds

        [BsonElement("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
