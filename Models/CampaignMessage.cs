using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.MailboxService.Models;

[BsonIgnoreExtraElements]
public class CampaignMessage : MailboxMessage
{
}