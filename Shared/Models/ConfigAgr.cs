using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Models
{
    public class ConfigAgr
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("id")]
        public string AgrId { get; set; } = string.Empty;

        [BsonElement("port")]
        public int Port { get; set; }
    }
}
