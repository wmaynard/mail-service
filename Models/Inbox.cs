using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.MailboxService.Models;

[BsonIgnoreExtraElements]
public class Inbox : PlatformCollectionDocument
{
    internal const string DB_KEY_ACCOUNT_ID = "aid";
    internal const string DB_KEY_MESSAGES = "msgs";
    internal const string DB_KEY_TIMESTAMP = "tmestmp";
    internal const string DB_KEY_HISTORY = "hist";

    public const string FRIENDLY_KEY_ACCOUNT_ID = "accountId";
    public const string FRIENDLY_KEY_MESSAGES = "messages";
    public const string FRIENDLY_KEY_TIMESTAMP = "timestamp";
    public const string FRIENDLY_KEY_HISTORY = "history";
    
    [AdditionalIndexKey(group: "INDEX_GROUP_INBOX", key: "_id", priority: 0)]
    [SimpleIndex]
    [CompoundIndex(group: "INDEX_GROUP_INBOX", priority: 1)]
    [BsonElement(DB_KEY_ACCOUNT_ID)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ACCOUNT_ID)]
    public string AccountId { get; private set; }
    
    [BsonElement(DB_KEY_MESSAGES)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_MESSAGES)]
    public List<MailboxMessage> Messages { get; set; }

    [BsonElement(DB_KEY_TIMESTAMP)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
    public long Timestamp { get; private set; }
    
    [CompoundIndex(group: "INDEX_GROUP_INBOX", priority: 1)]
    [BsonElement(DB_KEY_HISTORY)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_HISTORY)]
    public List<MailboxMessage> History { get; private set; }

    [JsonConstructor]
    public Inbox() { }

    public Inbox(string aid, List<MailboxMessage> messages, List<MailboxMessage> history, long timestamp = 0, string id = null)
    {
        AccountId = aid;
        Messages = messages;
        Timestamp = timestamp == 0 ? Common.Utilities.Timestamp.UnixTime : timestamp;
        History = history 
            ?? messages?.Copy() 
            ?? new List<MailboxMessage>(); // This might (?) be able to replace the CreateHistory() method.
        
        if (id != null)
        {
            Id = id;
        }
    }

    // workaround for if inbox was created without history
    public void CreateHistory()
    {
        History = new List<MailboxMessage>();
        History.AddRange(Messages);
    }
}