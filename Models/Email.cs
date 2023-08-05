using MongoDB.Bson.Serialization.Attributes;

namespace LogFetcher.Models
{
    public class Email
    {
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("From")]
        public string From { get; set; }

        [BsonElement("To")]
        public string To { get; set; }

        [BsonElement("Subject")]
        public string Subject { get; set; }

        [BsonElement("Body")]
        public string Body { get; set; }
    }
}
