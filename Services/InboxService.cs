using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class InboxService : MinqService<Inbox>
{
    public InboxService() : base("inboxes") { }

    // TODO: Grab their messages
    public override Inbox FromId(string accountId) => mongo
        .Where(query => query.EqualTo(inbox => inbox.AccountId, accountId))
        .Upsert(query => query
            .Set(inbox => inbox.AccountId, accountId)
            .SetToCurrentTimestamp(inbox => inbox.LastAccessed)
            .SetOnInsert(inbox => inbox.CreatedOn, Timestamp.Now)
        );
}