using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.Common.Web;
using Timer = System.Timers.Timer;

namespace Rumble.Platform.MailboxService.Services
{
    public class InboxService : PlatformMongoService<Inbox>
    {
        private readonly Timer _inboxTimer;

        public void DeleteExpired(Inbox inbox) // deletes old expired messages TODO fix
        {
            long timestamp = Inbox.UnixTime; // again model to get unixtime..
            List<Message> expiredMessages = inbox.Messages.Where(message => message.VisibleFrom < timestamp && message.Expiration > timestamp).ToList();
            foreach (Message expiredMessage in expiredMessages)
            {
                // need to update isExpired? or is this automatic TODO
            }
        }

        private void CheckExpiredInbox(object sender, ElapsedEventArgs args) // tbh just guided from chat-service
        {
            _inboxTimer.Start();
            try
            {
                Log.Local(Owner.Nathan, message:"Attempt to delete expired messages...");
                DeleteExpired();
            }
            catch (Exception e)
            {
                Log.Local(Owner.Nathan, message:"Failure to delete expired messages.", exception: e);
            }
            _inboxTimer.Start();
        }
        
        public InboxService() : base(collection: "inboxes")
        {
            _inboxTimer = new Timer(interval: int.Parse(PlatformEnvironment.Variable(name:"INBOX_CHECK_FREQUENCY_SECONDS") ?? "3600")) // check every hour
            {
                AutoReset = true
            };
            _inboxTimer.Elapsed += CheckExpiredInbox; // TODO check implementation
            _inboxTimer.Start();
        }
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.