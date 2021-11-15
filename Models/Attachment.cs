using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models
{
    public class Attachment : PlatformDataModel
    {
        internal const string DB_KEY_QUANTITY = "qnty";
        internal const string DB_KEY_TYPE = "type";

        public const string FRIENDLY_KEY_QUANTITY = "quantity";
        public const string FRIENDLY_KEY_TYPE = "type";
        
        [BsonElement(DB_KEY_QUANTITY)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_QUANTITY)]
        public int Quantity { get; private set; }
        
        // TODO probably change to enum once we know what types this can be
        [BsonElement(DB_KEY_TYPE)]
        [JsonInclude, JsonPropertyName(FRIENDLY_KEY_TYPE)]
        public string Type { get; private set; }

        // TODO also change string to whatever type we decide on
        public Attachment(int quantity, string type)
        {
            Quantity = quantity;
            Type = type;
        }
    }
}

// Attachment - tentative
// - Quantity
// - Type (string for now)