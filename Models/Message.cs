using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models;

[BsonIgnoreExtraElements]
public class Message : PlatformCollectionDocument
{
    internal const string DB_KEY_SUBJECT = "sbjct";
    internal const string DB_KEY_BODY = "body";
    internal const string DB_KEY_ATTACHMENTS = "attchmnts";
    internal const string DB_KEY_TIMESTAMP = "tmestmp";
    internal const string DB_KEY_DATA = "data";
    internal const string DB_KEY_EXPIRATION = "expire";
    internal const string DB_KEY_VISIBLE_FROM = "visible";
    internal const string DB_KEY_ICON = "icon";
    internal const string DB_KEY_BANNER = "banner";
    internal const string DB_KEY_STATUS = "status";
    internal const string DB_KEY_INTERNAL_NOTE = "note";
    internal const string DB_KEY_PREVIOUS_VERSIONS = "prev";

    public const string FRIENDLY_KEY_SUBJECT = "subject";
    public const string FRIENDLY_KEY_BODY = "body";
    public const string FRIENDLY_KEY_ATTACHMENTS = "attachments";
    public const string FRIENDLY_KEY_DATA = "data";
    public const string FRIENDLY_KEY_TIMESTAMP = "timestamp";
    public const string FRIENDLY_KEY_EXPIRATION = "expiration";
    public const string FRIENDLY_KEY_VISIBLE_FROM = "visibleFrom";
    public const string FRIENDLY_KEY_ICON = "icon";
    public const string FRIENDLY_KEY_BANNER = "banner";
    public const string FRIENDLY_KEY_STATUS = "status";
    public const string FRIENDLY_KEY_INTERNAL_NOTE = "internalNote";
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
    
    [BsonElement(DB_KEY_DATA), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_DATA), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GenericData Data { get; set; }
    
    [BsonElement(DB_KEY_TIMESTAMP)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
    public long Timestamp { get; private set; }
    
    [BsonElement(DB_KEY_EXPIRATION)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRATION)]
    public long Expiration { get; private set; }
    
    [BsonElement(DB_KEY_VISIBLE_FROM)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_VISIBLE_FROM)]
    public long VisibleFrom { get; private set; }
    
    [BsonElement(DB_KEY_ICON), BsonDefaultValue(null)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ICON)]
    public string Icon { get; private set; }
    
    [BsonElement(DB_KEY_BANNER), BsonDefaultValue(null)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_BANNER)]
    public string Banner { get; private set; }
    
    public enum StatusType { UNCLAIMED, CLAIMED }
    [BsonElement(DB_KEY_STATUS)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
    public StatusType Status { get; private set; }
     
    // This field generates telemetry entries when players claim their messages.
    // This comes in to economysource.transaction_context and is used in SQL queries for analysis.
    // Examples include:
    // 
    // Admin Portal:  MSG-62215869806324548d612eb4 CSGrant-Reason-release_check-03-03-2
    // Leaderboards:  MSG-62215869806324548d612eb4 LB-621d7b50ed456b3870d05a4c Tier-0 Score-188 Rank-1
    [BsonElement(DB_KEY_INTERNAL_NOTE), BsonDefaultValue(null)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_INTERNAL_NOTE)]
    public string InternalNote { get; private set; }
    
    [BsonElement(DB_KEY_PREVIOUS_VERSIONS), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PREVIOUS_VERSIONS)]
    public List<Message> PreviousVersions { get; private set; }

    [BsonIgnore]
    [JsonIgnore]
    public bool IsExpired => Expiration <= UnixTime; // no setter, change expiration to UnixTime instead

    public Message()
    {
        Icon = "";
        Banner = "";
        PreviousVersions = new List<Message>();
        Id = ObjectId.GenerateNewId().ToString(); // Will: probably not needed; mongo assigns one when inserting (but not upserting).
        Timestamp = UnixTime;
    }
    
    // TODO: This can probably be removed once the update endpoint is refactored.
    public void UpdateBase(string subject, string body, IEnumerable<Attachment> attachments, long expiration,
        long visibleFrom, string icon, string banner, StatusType status, string internalNote)
    {
        Subject = subject;
        Body = body;
        Attachments = attachments.ToList();
        Timestamp = UnixTime;
        Expiration = expiration;
        VisibleFrom = visibleFrom;
        Icon = icon;
        Banner = banner;
        Status = status;
        InternalNote = internalNote;
    }

    public void Expire() => Expiration = UnixTime;

    public void UpdateClaimed() => Status = (Status == StatusType.UNCLAIMED)
        ? StatusType.CLAIMED
        : throw new PlatformException(message:$"Message {Id} has already been claimed!");
    
    public void RemovePrevious() => PreviousVersions = null;

    public void UpdatePrevious(Message message)
    {
        List<Message> oldPrevious = message.PreviousVersions;
        message.RemovePrevious();
        oldPrevious.Add(message);
        PreviousVersions.AddRange(oldPrevious);
    }

    public void Validate() // add future validations here
    {
        // DRY - don't repeat yourself
        long ConvertUnixMStoS(long value)
        {
            if (value < 10_000_000_000_000 && value >= 1_000_000_000_000) // more efficient than converting to string and checking length
                return value / 1_000;                                     // convert from ms to s by dropping last 3 digits
            if (value < 1_000_000_000 || value >= 10_000_000_000)         // in case neither ms or s unix time (not 13 or 10 digits)
                throw new PlatformException(message: "Timestamp is not a Unix timestamp (either in seconds or in milliseconds).");
            return value;
        }
        Timestamp = ConvertUnixMStoS(Timestamp);
        Expiration = ConvertUnixMStoS(Expiration);
        VisibleFrom = ConvertUnixMStoS(VisibleFrom);
    }
}

// Message
// - Subject
// - Body
// - Collection of Attachments
// - Timestamp (Unix timestamp, assigned on creation)
// - Expiration (Unix timestamp)
// - VisibleFrom (Unix timestamp)
// - Icon (string value)
// - Banner (string value)
// - Status (CLAIMED or UNCLAIMED)
// - IsExpired (getter property)