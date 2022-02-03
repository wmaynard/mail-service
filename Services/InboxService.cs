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

        public void DeleteExpired() // removes old expired messages in inboxes
        {
            //just keeping the ones that are not expired and are visible
            long expireTime = Inbox.UnixTime + long.Parse(PlatformEnvironment.Variable(name: "INBOX_DELETE_OLD_SECONDS") ?? "604800") * 1000;
            
            List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
            
            FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Expiration > expireTime);
            UpdateDefinition<Inbox> update = Builders<Inbox>.Update.PullFilter(inbox => inbox.Messages, message => message.Expiration > expireTime);

            listWrites.Add(new UpdateManyModel<Inbox>(filter, update));

            _collection.BulkWrite(listWrites);
        }

        private void CheckExpiredInbox(object sender, ElapsedEventArgs args)
        {
            _inboxTimer.Start();
            try
            {
                DeleteExpired();
            }
            catch (Exception e)
            {
                Log.Error(owner: Owner.Nathan, message:"Failure to check expired messages.", data: $"{e.Message}");
            }
            _inboxTimer.Start();
        }
        
        public InboxService() : base(collection: "inboxes")
        {
            _inboxTimer = new Timer(interval: int.Parse(PlatformEnvironment.Variable(name:"INBOX_CHECK_FREQUENCY_SECONDS") ?? "3600") * 1000) // check every hour
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

        public void UpdateAll(string id, GlobalMessage edited)
        {
            List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
            
            FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Id == id);
            UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Set(inbox => inbox.Messages[-1], edited);
            FilterDefinition<Inbox> filterHistory = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.History, message => message.Id == id);
            UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Set(inbox => inbox.History[-1], edited);

            listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
            listWrites.Add(new UpdateManyModel<Inbox>(filterHistory, updateHistory));
            _collection.BulkWrite(listWrites);
        }

        public void UpdateExpiration(string id)
        {
            List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
            
            FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Id == id);
            UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Set(inbox => inbox.Messages[-1].Expiration, Inbox.UnixTime);
            FilterDefinition<Inbox> filterHistory = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.History, message => message.Id == id);
            UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Set(inbox => inbox.History[-1].Expiration, Inbox.UnixTime);

            listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
            listWrites.Add(new UpdateManyModel<Inbox>(filterHistory, updateHistory));
            _collection.BulkWrite(listWrites);
        }

        public void SendTo(List<string> accountIds, Message message)
        {
            List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
            
            FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.In(inbox => inbox.AccountId, accountIds);
            UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Push(inbox => inbox.Messages, message);
            UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Push(inbox => inbox.History, message);

            listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
            listWrites.Add(new UpdateManyModel<Inbox>(filter, updateHistory));
            _collection.BulkWrite(listWrites);
        }
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.
