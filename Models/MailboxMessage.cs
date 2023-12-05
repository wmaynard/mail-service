using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Amazon.Auth.AccessControlPolicy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.MailboxService.Models;

[BsonIgnoreExtraElements]
public class MailboxMessage : PlatformCollectionDocument
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

    internal const string DB_KEY_FOR_ACCOUNTS_BEFORE = "acctsbefore";

    public const string FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE = "forAccountsBefore";
    
    [BsonElement(DB_KEY_FOR_ACCOUNTS_BEFORE), BsonIgnoreIfDefault]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ForAccountsBefore { get; protected set; }
    
    public const string FRIENDLY_KEY_RECIPIENT = "accountId";
    
    [BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECIPIENT), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Recipient { get; set; }
    
    [BsonElement("gid"), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName("globalMessageId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string GlobalMessageId { get; set; }
    
    [BsonElement(DB_KEY_SUBJECT)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_SUBJECT)]
    public string Subject { get; protected set; }
    
    [BsonElement(DB_KEY_BODY)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_BODY)]
    public string Body { get; protected set; }
    
    [BsonElement(DB_KEY_ATTACHMENTS)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ATTACHMENTS)]
    public List<Attachment> Attachments { get; protected set;}
    
    [BsonElement(DB_KEY_DATA), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_DATA), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RumbleJson Data { get; set; }
    
    [BsonElement(DB_KEY_EXPIRATION)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRATION)]
    public long Expiration { get; protected set; }
    
    [BsonElement(DB_KEY_VISIBLE_FROM)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_VISIBLE_FROM)]
    public long VisibleFrom { get; protected set; }
    
    [BsonElement(DB_KEY_ICON), BsonDefaultValue(null)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ICON)]
    public string Icon { get; protected set; }
    
    [BsonElement(DB_KEY_BANNER), BsonDefaultValue(null)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_BANNER)]
    public string Banner { get; protected set; }
    
    public enum StatusType { UNCLAIMED, CLAIMED }
    
    [BsonElement(DB_KEY_STATUS)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_STATUS)]
    public StatusType Status { get; protected set; }
     
    // This field generates telemetry entries when players claim their messages.
    // This comes in to economysource.transaction_context and is used in SQL queries for analysis.
    // Examples include:
    // 
    // Admin Portal:  MSG-62215869806324548d612eb4 CSGrant-Reason-release_check-03-03-2
    // Leaderboards:  MSG-62215869806324548d612eb4 LB-621d7b50ed456b3870d05a4c Tier-0 Score-188 Rank-1
    [BsonElement(DB_KEY_INTERNAL_NOTE), BsonDefaultValue(null)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_INTERNAL_NOTE)]
    public string InternalNote { get; protected set; }
    

    [BsonIgnore]
    [JsonIgnore]
    public bool IsExpired => Expiration <= Timestamp.Now; // no setter, change expiration to UnixTime instead
    
    internal const string DB_KEY_PROMO_CODE = "promo";
    public const string FRIENDLY_KEY_PROMO_CODE = "claimCode";
    
    [BsonElement("minAge")]
    [JsonInclude, JsonPropertyName("minimumAgeInSeconds")]
    public long MinimumAccountAge { get; set; }
    
    [BsonElement(DB_KEY_PROMO_CODE), BsonIgnoreIfNull]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_PROMO_CODE)]
    public string PromoCode { get; set; }
    
    [BsonElement("redirect")]
    [JsonInclude, JsonPropertyName("redirectUrl")]
    public string RedirectUrl { get; set; }

    public MailboxMessage()
    {
        Icon = "";
        Banner = "";
        Id = ObjectId.GenerateNewId().ToString();
    }

    public void Expire() => Expiration = Timestamp.Now;

    public void UpdateClaimed()
    {
        Status = (Status == StatusType.UNCLAIMED)
             ? StatusType.CLAIMED
             : throw new PlatformException(message: $"Message has already been claimed!");
    }

    protected override void Validate(out List<string> errors)
    {
        errors = new List<string>();
        Attachments ??= new List<Attachment>();
        
        if (string.IsNullOrWhiteSpace(Subject))
            errors.Add("A subject must be provided.");
        if (string.IsNullOrWhiteSpace(Body))
            errors.Add("A body must be provided.");
        if (this is not CampaignMessage)
            return;
        
        if (string.IsNullOrWhiteSpace(PromoCode))
            errors.Add("A claim code must be provided.");
        if (!Attachments.Any())
            errors.Add("Campaign rewards must have at least one attachment");
        if (Expiration == 0)
            Expiration = Timestamp.InTheFuture(years: 30);
    }
}