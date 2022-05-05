using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Web;

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

    // TODO also change string to whatever type we decide on
    public Attachment(string type, string rewardId, int quantity = 1)
    {
        Type = type;
        RewardId = rewardId;
        Quantity = quantity;
        
        // TODO
        // Probably better to throw an exception when quantity is 0.  However, I don't know if there's a specific use case for this.
        // Letting a bad request through by defaulting the value to a valid one may be more confusing than seeing errors.
        if (quantity == 0)
            Quantity = 1;
    }
}

// Attachment - tentative
// - Type (string for now)
// - RewardId
// - Quantity