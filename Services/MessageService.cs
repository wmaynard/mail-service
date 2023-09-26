using System.Collections.Generic;
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
            .LessThanOrEqualTo(message => message.VisibleFrom, Timestamp.UnixTime)
            .GreaterThanOrEqualTo(message => message.Expiration, Timestamp.UnixTime)
        )
        .ToArray();

    public long Claim(string accountId, string messageId = null) => string.IsNullOrWhiteSpace(messageId)
        ? mongo
            .Where(query => query.EqualTo(message => message.Recipient, accountId))
            .Update(query => query.Set(message => message.Status, MailboxMessage.StatusType.CLAIMED))
        : mongo
            .Where(query => query
                .EqualTo(message => message.Recipient, accountId)
                .EqualTo(message => message.Id, messageId)
            )
            .Update(query => query.Set(message => message.Status, MailboxMessage.StatusType.CLAIMED));
    
    protected override void OnElapsed()
    {
        const long ONE_WEEK = 604_800;
        long offset = Optional<DynamicConfig>()
            .Optional<long?>("INBOX_DELETE_OLD_SECONDS")
            ?? ONE_WEEK;
        
        long affected = mongo
            .Where(query => query.LessThanOrEqualTo(message => message.Expiration, Timestamp.UnixTime - offset))
            .Delete();
        
        if (affected > 0)
            Log.Info(Owner.Will, "Old mailbox messages deleted.", data: new
            {
                Affected = affected
            });
    }
}