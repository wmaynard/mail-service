using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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
        internal const string DB_KEY_PREVIOUS_VERSIONS = "prev";

        public const string FRIENDLY_KEY_SUBJECT = "subject";
        public const string FRIENDLY_KEY_BODY = "body";
        public const string FRIENDLY_KEY_ATTACHMENTS = "attachments";
        public const string FRIENDLY_KEY_TIMESTAMP = "timestamp";
        public const string FRIENDLY_KEY_EXPIRATION = "expiration";
        public const string FRIENDLY_KEY_VISIBLE_FROM = "visibleFrom";
        public const string FRIENDLY_KEY_IMAGE = "image";
        public const string FRIENDLY_KEY_STATUS = "status";
        public const string FRIENDLY_KEY_PREVIOUS_VERSIONS = "previousVersions";

        [BsonElement(DB_KEY_SUBJECT)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_SUBJECT)]
        public string Subject { get; private set; }
        
        [BsonElement(DB_KEY_BODY)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_BODY)]
        public string Body { get; private set; }
        
        [BsonElement(DB_KEY_ATTACHMENTS)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ATTACHMENTS)]
        public List<Attachment> Attachments { get; private set;}
        
        [BsonElement(DB_KEY_TIMESTAMP)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
        public long Timestamp { get; private set; }
        
        [BsonElement(DB_KEY_EXPIRATION)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRATION)]
        public long Expiration { get; private set; }
        
        [BsonElement(DB_KEY_VISIBLE_FROM)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_VISIBLE_FROM)]
        public long VisibleFrom { get; private set; }
        
        [BsonElement(DB_KEY_IMAGE)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_IMAGE)]
        public string Image { get; private set; }
        
        public enum StatusType { UNCLAIMED, CLAIMED }
        [BsonElement(DB_KEY_STATUS)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
        public StatusType Status { get; private set; }
        
        [BsonElement(DB_KEY_PREVIOUS_VERSIONS), BsonIgnoreIfNull]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PREVIOUS_VERSIONS)]
        public List<Message> PreviousVersions { get; private set; }

        [BsonIgnore]
        [JsonIgnore]
        public bool IsExpired => Expiration <= UnixTime; // no setter, change expiration to UnixTime instead

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
            PreviousVersions = new List<Message>();
            Id = ObjectId.GenerateNewId().ToString(); // potential overlap with GlobalMessage?
        }
        
        public void UpdateBase(string subject, string body, List<Attachment> attachments, long expiration,
            long visibleFrom, string image, StatusType status)
        {
            Subject = subject;
            Body = body;
            Attachments = attachments;
            Timestamp = UnixTime;
            Expiration = expiration;
            VisibleFrom = visibleFrom;
            Image = image;
            Status = status;
        }

        public void ExpireBase()
        {
            Expiration = UnixTime;
        }

        public void UpdateClaimed()
        {
            if (Status == StatusType.UNCLAIMED)
            {
                Status = StatusType.CLAIMED;
            }
            else
            {
                throw new Exception(message:$"Message {Id} has already been claimed!");
            }
        }

        public void RemovePrevious()
        {
            PreviousVersions = null;
        }

        public void UpdatePrevious(Message message)
        {
            List<Message> oldPrevious = message.PreviousVersions;
            message.RemovePrevious();
            oldPrevious.Add(message);
            PreviousVersions.AddRange(oldPrevious);
        }

        public void SetId(string id)
        {
            Id = id;
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