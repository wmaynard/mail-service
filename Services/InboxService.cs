using System.Collections.Generic;
using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class MinqInboxService : MinqService<Inbox>
{
    public MinqInboxService() : base("inboxes") { }

    // TODO: Grab their messages
    public override Inbox FromId(string accountId) => mongo
        .Where(query => query.EqualTo(inbox => inbox.AccountId, accountId))
        .Upsert(query => query
            .Set(inbox => inbox.AccountId, accountId)
            .SetToCurrentTimestamp(inbox => inbox.LastAccessed)
            .SetOnInsert(inbox => inbox.CreatedOn, Timestamp.UnixTime)
        );
}



