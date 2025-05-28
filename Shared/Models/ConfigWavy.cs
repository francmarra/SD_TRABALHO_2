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

        [BsonElement("status")]
        public int Status { get; set; }

        [BsonElement("last_sync")]
        public DateTime LastSync { get; set; }
    }
}
