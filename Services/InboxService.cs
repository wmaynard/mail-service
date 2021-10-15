using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using MongoDB.Driver;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;
using Rumble.Platform.MailboxService.Models;
using Timer = System.Timers.Timer;

namespace Rumble.Platform.MailboxService.Services
{
    public class InboxService : PlatformMongoService<Inbox>
    {
        private Timer _inboxTimer;

        public InboxService() : base(collection: "inboxes")
        {
            _inboxTimer = new Timer(interval: int.Parse(RumbleEnvironment.Variable("INBOX_CHECK_FREQUENCY_SECONDS") ?? "10"))
            {
                AutoReset = true
            };
            _inboxTimer.Elapsed += CheckExpiredInbox;
            _inboxTimer.Start();
        }
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.