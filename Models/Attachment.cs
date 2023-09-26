using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeAttributes

namespace Rumble.Platform.MailboxService.Models;

[BsonIgnoreExtraElements]
public class Attachment : PlatformDataModel
{
    internal const string DB_KEY_TYPE = "type";
    internal const string DB_KEY_REWARD_ID = "rwdId";
    internal const string DB_KEY_QUANTITY = "qnty";

    public const string FRIENDLY_KEY_TYPE = "type";
    public const string FRIENDLY_KEY_REWARD_ID = "rewardId";
    public const string FRIENDLY_KEY_QUANTITY = "quantity";
    
    // TODO probably change to enum once we know what types this can be
    [BsonElement(DB_KEY_TYPE)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TYPE)]
    public string Type { get; private set; }
    
    [BsonElement(DB_KEY_REWARD_ID)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_REWARD_ID)]
    public string RewardId { get; private set; }
    
    [BsonElement(DB_KEY_QUANTITY)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_QUANTITY)]
    public int Quantity { get; private set; }

    protected override void Validate(out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Type))
            errors.Add($"{FRIENDLY_KEY_TYPE} must be a non-empty string.");
        if (string.IsNullOrWhiteSpace(RewardId))
            errors.Add($"{FRIENDLY_KEY_REWARD_ID} must be a non-empty string.");
        if (Quantity <= 0)
            errors.Add($"{FRIENDLY_KEY_QUANTITY} must be greater than 0.");
    }
}