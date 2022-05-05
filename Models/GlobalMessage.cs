using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.MailboxService.Models;

[BsonIgnoreExtraElements]
public class GlobalMessage : Message
{
    internal const string DB_KEY_FOR_ACCOUNTS_BEFORE = "acctsbefore";

    public const string FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE = "forAccountsBefore";
    
    [BsonElement(DB_KEY_FOR_ACCOUNTS_BEFORE)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_FOR_ACCOUNTS_BEFORE)]
    public long? ForAccountsBefore { get; private set; }


    // TODO: This can probably be removed once the update endpoint is refactored.
    public void UpdateGlobal(string subject, string body, IEnumerable<Attachment> attachments, long expiration,
        long visibleFrom, string icon, string banner, StatusType status, string internalNote, long? forAccountsBefore = null)
    {
        ForAccountsBefore = forAccountsBefore;
        UpdateBase(subject: subject, body: body, attachments: attachments, expiration: expiration, 
            visibleFrom: visibleFrom, icon: icon, banner: banner, status: status, internalNote: internalNote);
    }
}

// GlobalMessage : Message
// - EligibleForNewAccounts (bool)