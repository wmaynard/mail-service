using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models
{
    public class Inbox : PlatformCollectionDocument
    {
        internal const string DB_KEY_ACCOUNT_ID = "aid";
        internal const string DB_KEY_MESSAGES = "msgs";

        public const string FRIENDLY_KEY_ACCOUNT_ID = "accoundId";
        public const string FRIENDLY_KEY_MESSAGES = "messages";
        
        [BsonElement(DB_KEY_ACCOUNT_ID)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ACCOUNT_ID)]
        public string AccountId { get; private set; }
        
        [BsonElement(DB_KEY_MESSAGES)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_MESSAGES)]
        public List<Message> Messages { get; private set; }

        public Inbox(string aid, List<Message> messages)
        {
            AccountId = aid;
            Messages = messages;
        }
    }
}

// Inbox
// - Linked to players by their accountId (aid)
// - Collection of Messages