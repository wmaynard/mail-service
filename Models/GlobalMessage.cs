using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.MailboxService.Models
{
    public class GlobalMessage : Message
    {
        internal const string DB_KEY_FOR_ACCOUNTS_BEFORE = "acctsbefore";
        internal const string DB_KEY_ATTACHMENT = "attchmnt";

        public const string FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE = "forAccountsBefore";
        public const string FRIENDLY_KEY_ATTACHMENT = "attachment";
        
        [BsonElement(DB_KEY_FOR_ACCOUNTS_BEFORE)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE)]
        public long? ForAccountsBefore { get; private set; }
        
        [BsonElement(DB_KEY_ATTACHMENT)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ATTACHMENT)]
        public Attachment Attachment { get; private set; }

        public GlobalMessage(string subject, string body, List<Attachment> attachments, long expiration,
            long visibleFrom, string image, StatusType status, Attachment attachment, long? forAccountsBefore = null) 
            : base(subject: subject, body: body, attachments: attachments, expiration: expiration, 
                visibleFrom: visibleFrom, image: image)
        {
            Attachment = attachment; // optional attachments? also attachments already in messages
            ForAccountsBefore = forAccountsBefore;
        }
        
        public void UpdateGlobal(string subject, string body, List<Attachment> attachments, long expiration,
            long visibleFrom, string image, StatusType status, Attachment attachment, long? forAccountsBefore = null)
        {
            Attachment = attachment;
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
            Attachment attachment = message.Attachment;
            long? forAccountsBefore = message.ForAccountsBefore;
            GlobalMessage copy = new GlobalMessage(subject: subject, body: body, attachments: attachments, expiration: expiration, visibleFrom: visibleFrom, 
                image: image, status: status, attachment: attachment, forAccountsBefore: forAccountsBefore);
            copy.SetId(message.Id);
            return copy;
        }
    }
}

// GlobalMessage : Message
// - EligibleForNewAccounts (bool)
// - Attachment - reasonable to think this should be a collection of attachments instead?