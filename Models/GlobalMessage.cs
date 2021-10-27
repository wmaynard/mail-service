using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Rumble.Platform.MailboxService.Models
{
    public class GlobalMessage : Message
    {
        internal const string DB_KEY_FOR_ACCOUNTS_BEFORE = "acctsbefore";
        internal const string DB_KEY_ATTACHMENT = "attchmnt";

        public const string FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE = "forAccountsBefore";
        public const string FRIENDLY_KEY_ATTACHMENT = "attachment";
        
        [BsonElement(DB_KEY_FOR_ACCOUNTS_BEFORE)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE)]
        public long? ForAccountsBefore { get; private set; }
        
        [BsonElement(DB_KEY_ATTACHMENT)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ATTACHMENT)]
        public Attachment Attachment { get; private set; }

        public GlobalMessage(string subject, string body, List<Attachment> attachments, long expiration, // too long
            long visibleFrom, string image, StatusType status, Attachment attachment, long? forAccountsBefore = null) 
            : base(subject: subject, body: body, attachments: attachments, expiration: expiration, 
                visibleFrom: visibleFrom, image: image, status: status)
        {
            Attachment = attachment; // optional attachments? also attachments already in messages
            ForAccountsBefore = forAccountsBefore;
        }
    }
}

// GlobalMessage : Message
// - EligibleForNewAccounts (bool)
// - Attachment - reasonable to think this should be a collection of attachments instead?