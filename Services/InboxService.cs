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
        private readonly Timer _inboxTimer;

        public void DeleteExpired() // removes old expired messages
        {
            long timestamp = Inbox.UnixTime; // again model to get unixtime..
            // perhaps just keep the ones that are not expired and are visible
            // cleaning all inboxes, maybe not optimal? TODO check
            IEnumerable<Inbox> allInboxes = List(); // iterating through IEnumerable or list? which is more efficient? TODO check
            foreach (Inbox inbox in allInboxes)
            {
                List<Message> unexpiredMessages = inbox.Messages
                    .Where(message => message.VisibleFrom <= timestamp && message.Expiration > timestamp).ToList();
                inbox.UpdateMessages(unexpiredMessages);
                Update(inbox);
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
            // TODO: Timers use MS, so this is actually running every 3.6 seconds
            _inboxTimer = new Timer(interval: int.Parse(PlatformEnvironment.Variable(name:"INBOX_CHECK_FREQUENCY_SECONDS") ?? "3600")) // check every hour
            {
                AutoReset = true
            };
            _inboxTimer.Elapsed += CheckExpiredInbox; // TODO check implementation
            _inboxTimer.Start();
        }
        
        // TODO: You'll need to implement a method to retrieve records by AccountId.  The AccountId is not the same as the MongoDB _id.
        // Something like: 
        // public override Inbox Get(string accountId) => _collection.Find(inbox => inbox.AccountId == accountId).FirstOrDefault();
        // Otherwise, you will end up with a new inbox for the account every time you try and retrieve it.
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.