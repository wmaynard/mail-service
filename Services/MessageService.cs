using System.Linq;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class MessageService : MinqTimerService<MailboxMessage>
{
    public MessageService() : base("messages", interval: 86_400_000) { } // Once daily

    public void Grant(MailboxMessage message, params string[] accountIds) => mongo.Insert(accountIds.Select(id =>
    {
        MailboxMessage output = message.Copy();
        output.ChangeId();
        output.Recipient = id;
        return output;
    }).ToArray());

    public long GrantGlobals(MailboxMessage[] globals)
    {
        if (!globals.Any())
            return 0;
        if (globals.Any(message => string.IsNullOrWhiteSpace(message.Recipient)))
            throw new PlatformException("Global message grants must have an account ID.");
        
        string[] existing = mongo
            .Where(query => query.EqualTo(message => message.Recipient, globals.First().Recipient))
            .And(query => query.ContainedIn(message => message.GlobalMessageId, globals.Select(message => message.GlobalMessageId)))
            .Project(message => message.GlobalMessageId)
            .ToArray();

        MailboxMessage[] toInsert = globals
            .Where(global => !string.IsNullOrWhiteSpace(global.GlobalMessageId))
            .Where(global => !existing.Contains(global.GlobalMessageId))
            .ToArray();
        
        mongo.Insert(toInsert);
        
        return toInsert.Length;
    }

    public MailboxMessage Expire(string messageId) => mongo
        .Where(query => query.EqualTo(message => message.Id, messageId))
        .Upsert(query => query.SetToCurrentTimestamp(message => message.Expiration));

    public void UpdateOne(MailboxMessage edited) => mongo.Replace(edited);

    public long UpdateMany(MailboxMessage other) => mongo
        .Where(query => query.EqualTo(message => message.GlobalMessageId, other.GlobalMessageId))
        .Or(query => query.EqualTo(message => message.Id, other.Id))
        .Update(query => query
            .Set(message => message.Subject, other.Subject)
            .Set(message => message.GlobalMessageId, other.GlobalMessageId)
            .Set(message => message.Attachments, other.Attachments)
            .Set(message => message.Body, other.Body)
            .Set(message => message.Expiration, other.Expiration)
            .Set(message => message.VisibleFrom, other.VisibleFrom)
            .Set(message => message.Icon, other.Icon)
            .Set(message => message.Banner, other.Banner)
            .Set(message => message.Data, other.Data)
            .Set(message => message.InternalNote, other.InternalNote)
        );

    public MailboxMessage[] GetUnexpiredMessages(string accountId) => mongo
        .Where(query => query
            .EqualTo(message => message.Recipient, accountId)
            .LessThanOrEqualTo(message => message.VisibleFrom, Timestamp.Now)
            .GreaterThanOrEqualTo(message => message.Expiration, Timestamp.Now)
        )
        .ToArray();

    public MailboxMessage[] Claim(string accountId, string messageId = null)
    {
        MailboxMessage[] output = mongo
            .Where(query => query
                .EqualTo(message => message.Recipient, accountId)
                .LessThanOrEqualTo(message => message.VisibleFrom, Timestamp.Now)
                .GreaterThanOrEqualTo(message => message.Expiration, Timestamp.Now)
                .EqualTo(message => message.Status, MailboxMessage.StatusType.UNCLAIMED)
            )
            .And(query => query.EqualTo(message => message.Id, messageId), condition: !string.IsNullOrWhiteSpace(messageId))
            .Limit(100)
            .UpdateAndReturn(query => query.Set(message => message.Status, MailboxMessage.StatusType.CLAIMED), out long affected);
        
        if (affected == 0)
            Log.Warn(Owner.Will, "Tried to claim message(s), but no messages matching the request are available to claim.", data: new
            {
                Help = "This could be because a message had seconds to go when a player saw it in their inbox and expired, or tried to claim all messages when none were available",
                MessageId = messageId,
                AccountId = accountId
            });
        return output;
    }
    
    protected override void OnElapsed()
    {
        long affected = mongo
            .Where(query => query.LessThanOrEqualTo(message => message.Expiration, Timestamp.OneWeekAgo))
            .Delete();
        
        if (affected > 0)
            Log.Info(Owner.Will, "Old mailbox messages deleted.", data: new
            {
                Affected = affected
            });
    }
}