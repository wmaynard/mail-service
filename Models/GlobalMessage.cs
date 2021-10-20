using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Rumble.Platform.MailboxService.Models
{
    public class GlobalMessage : Message
    {
        internal const string DB_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS = "elgb";
        internal const string DB_KEY_ATTACHMENT = "attchmnt";

        public const string FRIENDLY_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS = "eligibleForNewAccounts";
        public const string FRIENDLY_KEY_ATTACHMENT = "attachment";
        
        [BsonElement(DB_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS)]
        public bool EligibleForNewAccounts { get; private set; }
        
        [BsonElement(DB_KEY_ATTACHMENT)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ATTACHMENT)]
        public Attachment Attachment { get; private set; }

        public GlobalMessage( // possibly no params and use object initializer instead?
            string subject,
            string body,
            List<Attachment> attachments,
            long expiration,
            long visibleFrom,
            string image,
            StatusType status,
            bool eligibleForNewAccounts,
            Attachment attachment) 
            : base(
            subject: subject,
            body: body,
            attachments: attachments,
            expiration: expiration,
            visibleFrom: visibleFrom,
            image: image,
            status: status)
        {
            EligibleForNewAccounts = eligibleForNewAccounts;
            Attachment = attachment;
        }
        
    }
}

// GlobalMessage : Message
// - EligibleForNewAccounts (bool)
// - Attachment - reasonable to think this should be a collection of attachments instead?