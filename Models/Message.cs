using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models
{
    public class Message : PlatformCollectionDocument
    {
        internal const string DB_KEY_SUBJECT = "sbjct";
        internal const string DB_KEY_BODY = "body";
        internal const string DB_KEY_ATTACHMENTS = "attchmnts";
        internal const string DB_KEY_TIMESTAMP = "tmestmp";
        internal const string DB_KEY_EXPIRATION = "expire";
        internal const string DB_KEY_VISIBLE_FROM = "visible";
        internal const string DB_KEY_IMAGE = "img";
        internal const string DB_KEY_STATUS = "status";

        public const string FRIENDLY_KEY_SUBJECT = "subject";
        public const string FRIENDLY_KEY_BODY = "body";
        public const string FRIENDLY_KEY_ATTACHMENTS = "attachments";
        public const string FRIENDLY_KEY_TIMESTAMP = "timestamp";
        public const string FRIENDLY_KEY_EXPIRATION = "expiration";
        public const string FRIENDLY_KEY_VISIBLE_FROM = "visibleFrom";
        public const string FRIENDLY_KEY_IMAGE = "image";
        public const string FRIENDLY_KEY_STATUS = "status";

        [BsonElement(DB_KEY_SUBJECT)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_SUBJECT)]
        public string Subject { get; private set; }
        
        [BsonElement(DB_KEY_BODY)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_BODY)]
        public string Body { get; private set; }
        
        [BsonElement(DB_KEY_ATTACHMENTS)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_ATTACHMENTS)]
        public List<Attachment> Attachments { get; private set;}
        
        [BsonElement(DB_KEY_TIMESTAMP)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_TIMESTAMP)]
        public long Timestamp { get; private set; }
        
        [BsonElement(DB_KEY_EXPIRATION)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_EXPIRATION)]
        public long Expiration { get; private set; }
        
        [BsonElement(DB_KEY_VISIBLE_FROM)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_VISIBLE_FROM)]
        public long VisibleFrom { get; private set; }
        
        [BsonElement(DB_KEY_IMAGE)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_IMAGE)]
        public string Image { get; private set; }
        
        public enum StatusType { UNCLAIMED, CLAIMED }
        [BsonElement(DB_KEY_STATUS)]
        [JsonProperty(PropertyName = FRIENDLY_KEY_STATUS)]
        public StatusType Status { get; private set; }

        [BsonIgnore]
        [JsonIgnore]
        public bool IsExpired => Expiration <= UnixTime; // no setter, current plan is to change expiration to currenttime

        public Message(string subject, string body, List<Attachment> attachments, long expiration, long visibleFrom, string image, StatusType status)
        {
            Subject = subject;
            Body = body;
            Attachments = attachments;
            Timestamp = UnixTime;
            Expiration = expiration;
            VisibleFrom = visibleFrom;
            Image = image;
            Status = status;
            Id = Guid.NewGuid().ToString();
        }

        public void Expire() // only actually used for globalmessages, but here to access expiration
        {
            Expiration = UnixTime;
        }

        public void UpdateClaimed() // message claimed, claimed should stop another claim attempt
        {
            if (Status == StatusType.UNCLAIMED)
            {
                Status = StatusType.CLAIMED;
            }
            else
            {
                throw new Exception(message:"Message has already been claimed!");
            }
        }
    }
}

// Message
// - Subject
// - Body
// - Collection of Attachments
// - Timestamp (Unix timestamp, assigned on creation)
// - Expiration (Unix timestamp)
// - VisibleFrom (Unix timestamp)
// - Image (string value)
// - Status (CLAIMED or UNCLAIMED)
// - IsExpired (getter property)