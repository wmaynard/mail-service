using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.MailboxService.Models
{
    public class GlobalMessage : Message
    {
        internal const string DB_KEY_FOR_ACCOUNTS_BEFORE = "acctsbefore";

        public const string FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE = "forAccountsBefore";
        
        [BsonElement(DB_KEY_FOR_ACCOUNTS_BEFORE)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE)]
        public long? ForAccountsBefore { get; private set; }

        public GlobalMessage(string subject, string body, List<Attachment> attachments, long expiration,
            long visibleFrom, string image, StatusType status, long? forAccountsBefore = null) 
            : base(subject: subject, body: body, attachments: attachments, expiration: expiration, 
                visibleFrom: visibleFrom, image: image)
        {
            ForAccountsBefore = forAccountsBefore;
        }
        
        public void UpdateGlobal(string subject, string body, List<Attachment> attachments, long expiration,
            long visibleFrom, string image, StatusType status, long? forAccountsBefore = null)
        {
            ForAccountsBefore = forAccountsBefore;
            UpdateBase(subject: subject, body: body, attachments: attachments, expiration: expiration, 
                visibleFrom: visibleFrom, image: image, status: status);
        }

        public void ExpireGlobal()
        {
            ExpireBase();
        }
        
        public static GlobalMessage CreateCopy(GlobalMessage message)
        {
            string subject = message.Subject;
            string body = message.Body;
            List<Attachment> attachments = message.Attachments;
            long expiration = message.Expiration;
            long visibleFrom = message.VisibleFrom;
            string image = message.Image;
            StatusType status = message.Status;
            long? forAccountsBefore = message.ForAccountsBefore;
            GlobalMessage copy = new GlobalMessage(subject: subject, body: body, attachments: attachments, expiration: expiration, visibleFrom: visibleFrom, 
                image: image, status: status, forAccountsBefore: forAccountsBefore);
            copy.SetId(message.Id);
            return copy;
        }
    }
}

// GlobalMessage : Message
// - EligibleForNewAccounts (bool)