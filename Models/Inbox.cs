using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Web;

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
    
    [BsonElement(DB_KEY_ACCOUNT_ID)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_ACCOUNT_ID)]
    public string AccountId { get; private set; }
    
    [BsonElement(DB_KEY_MESSAGES)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_MESSAGES)]
    public List<Message> Messages { get; private set; }

    [BsonElement(DB_KEY_TIMESTAMP)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TIMESTAMP)]
    public long Timestamp { get; private set; }
    
    [BsonElement(DB_KEY_HISTORY)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_HISTORY)]
    public List<Message> History { get; private set; }

    [JsonConstructor]
    public Inbox() { }

    public Inbox(string aid, List<Message> messages, List<Message> history, long timestamp = 0, string id = null)
    {
        AccountId = aid;
        Messages = messages;
        Timestamp = timestamp == 0 ? UnixTime : timestamp;
        History = history 
            ?? messages?.Copy() 
            ?? new List<Message>(); // This might (?) be able to replace the CreateHistory() method.
        
        if (id != null)
            Id = id;
    }
    
    public void UpdateMessages(List<Message> messages) => Messages = messages;  // This is good candidate for exposing the setter property to public - then this method can be removed.

    // workaround for if inbox was created without history
    public void CreateHistory()
    {
        History = new List<Message>();
        History.AddRange(Messages);
    }
}

// Inbox
// - Linked to players by their accountId (aid)
// - Collection of Messages
// - Timestamp of inbox creation
// - History of all messages