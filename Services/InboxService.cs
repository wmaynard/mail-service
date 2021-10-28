using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MongoDB.Bson;
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
            long timestamp = Inbox.UnixTime;
            // perhaps just keep the ones that are not expired and are visible
            // TODO problem where this pulls all inboxes at once, look into Mongo C# driver
            IEnumerable<Inbox> allInboxes = List();
            foreach (Inbox inbox in allInboxes)
            {
                List<Message> unexpiredMessages = inbox.Messages
                    .Where(message => message.VisibleFrom <= timestamp && message.Expiration + long.Parse(PlatformEnvironment.Variable(name:"INBOX_DELETE_OLD_SECONDS") ?? "604800000")> timestamp).ToList();
                inbox.UpdateMessages(unexpiredMessages);
                Update(inbox);
            }
        }

        private void CheckExpiredInbox(object sender, ElapsedEventArgs args)
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
            _inboxTimer = new Timer(interval: int.Parse(PlatformEnvironment.Variable(name:"INBOX_CHECK_FREQUENCY_SECONDS") ?? "3600000")) // check every hour
            {
                AutoReset = true
            };
            _inboxTimer.Elapsed += CheckExpiredInbox;
            _inboxTimer.Start();
        }
        
        public override Inbox Get(string accountId)
        {
            return _collection.Find(filter: inbox => inbox.AccountId == accountId).FirstOrDefault();
        }

        public void UpdateAll(string id)
        {
            _collection.UpdateMany();
        }

        public void UpdateExpiration(string id)
        {
            FilterDefinition<BsonDocument> filter = new BsonDocument(name:"Id", id);
            UpdateDefinition<BsonDocument> update = new BsonDocument(name: "$set", value: new BsonDocument(name:"Expiration", Inbox.UnixTime));
            _collection.UpdateMany(filter: filter, update: update);
            _collection.UpdateMany()
        }
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.
