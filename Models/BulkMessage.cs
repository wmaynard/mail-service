using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.MailboxService.Models;

public class BulkMessage : Message
{
	public const string FRIENDLY_KEY_RECIPIENT = "accountId";
	
	[BsonIgnore]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECIPIENT)]
	public string Recipient { get; set; }
}