using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Rumble.Platform.MailboxService.Models;

public class BulkMessage : Message
{
	public const string FRIENDLY_KEY_RECIPIENT = "accountId";
	
	[BsonIgnore]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_RECIPIENT), JsonRequired]
	public string Recipient { get; set; }
}