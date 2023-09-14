using System.Collections.Generic;
using System.Linq;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class MessageService : MinqService<MailboxMessage>
{
    public MessageService() : base("messages") { }

    public void Grant(MailboxMessage message, params string[] accountIds)
    {
        mongo.Insert(accountIds.Select(id =>
        {
            MailboxMessage output = message.Copy();
            output.ChangeId();
            output.Recipient = id;
            return output;
        }).ToArray());
    }

    public void Replace(MailboxMessage message) => mongo.Replace(message);

    public MailboxMessage Expire(string messageId) => mongo
        .Where(query => query.EqualTo(message => message.Id, messageId))
        .Upsert(query => query.SetToCurrentTimestamp(message => message.Expiration));

}