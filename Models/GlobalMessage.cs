using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models
{
    public class GlobalMessage : Message
    {
        internal const string DB_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS = "elgb";
        internal const string DB_KEY_ATTACHMENTS = "attchmnts";

        public const string FRIENDLY_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS = "eligibleForNewAccounts";
        public const string FRIENDLY_KEY_ATTACHMENTS = "attachments";
        
        [BsonElement(DB_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ELIGIBLE_FOR_NEW_ACCOUNTS)]
        public bool EligibleForNewAccounts { get; private set; }
        
        [BsonElement(DB_KEY_ATTACHMENTS)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ATTACHMENTS)]
        public List<Attachment> Attachments { get; private set; }

        public GlobalMessage(bool eligibleForNewAccounts, List<Attachment> attachments)
        {
            EligibleForNewAccounts = eligibleForNewAccounts;
            Attachments = attachments;
        }
        
    }
}

// GlobalMessage : Message
// - EligibleForNewAccounts (bool)
// - Attachment - reasonable to think this should be a collection of attachments instead?