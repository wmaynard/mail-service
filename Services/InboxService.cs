using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
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

    public void EnforceAccountAgeOver(string accountId, long seconds)
    {
        long created = mongo
            .Where(query => query.EqualTo(inbox => inbox.AccountId, accountId))
            .Limit(1)
            .Upsert()
            ?.CreatedOn
            ?? Timestamp.Now;

        if (created > Timestamp.Now - seconds)
            throw new PlatformException("Account is not old enough to be eligible for a reward", code: ErrorCode.Ineligible);
    }
}