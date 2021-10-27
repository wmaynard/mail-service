using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MongoDB.Driver;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.Common.Web;
using Timer = System.Timers.Timer;

namespace Rumble.Platform.MailboxService.Services
{
    public class InboxService : PlatformMongoService<Inbox>
    {
        public InboxService() : base(collection: "inboxes") { }
        
        public override Inbox Get(string accountId)
        {
            return _collection.Find(filter: inbox => inbox.AccountId == accountId).FirstOrDefault();
        }
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.