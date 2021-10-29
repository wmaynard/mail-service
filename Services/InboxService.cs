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
            //_collection.UpdateMany();
        }

        public void UpdateExpiration(string id)
        {
            List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
            
            FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, messages => messages.Id == id);
            
            UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Set(inbox => inbox.Messages[-1].Expiration, Inbox.UnixTime);

            listWrites.Add(new UpdateManyModel<Inbox>(filter, update));

            //FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.Empty; // if OOPS ACCIDENTALLY MESSED UP DOCUMENT
            //listWrites.Add(new DeleteOneModel<Inbox>(filter)); // then DELETE INBOX AND START AGAIN :(

            _collection.BulkWrite(listWrites);

            //_collection.UpdateMany(filter: filter, update: update);

        }
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.
