using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models
{
    public class Inbox : PlatformCollectionDocument
    {
        internal const string DB_KEY_ACCOUNT_ID = "aid";
        internal const string DB_KEY_MESSAGES = "msgs";
        internal const string DB_KEY_TIMESTAMP = "tmestmp";

        public const string FRIENDLY_KEY_ACCOUNT_ID = "accountId";
        public const string FRIENDLY_KEY_MESSAGES = "messages";
        public const string FRIENDLY_KEY_TIMESTAMP = "timestamp";
        
        [BsonElement(DB_KEY_ACCOUNT_ID)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ACCOUNT_ID)]
        public string AccountId { get; private set; }
        
        [BsonElement(DB_KEY_MESSAGES)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_MESSAGES)]
        public List<Message> Messages { get; private set; }
        
        [BsonElement(DB_KEY_TIMESTAMP)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_TIMESTAMP)]
        public long? Timestamp { get; private set; }

        public Inbox(string aid, List<Message> messages, long? timestamp = null)
        {
            AccountId = aid;
            Messages = messages;
            Timestamp = timestamp;
        }

        public void UpdateMessages(List<Message> messages)
        {
            Messages = messages;
        }
    }
}

// Inbox
// - Linked to players by their accountId (aid)
// - Collection of Messages