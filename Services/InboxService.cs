using System;
using System.Collections.Generic;
using System.Timers;
using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.Common.Web;
using Timer = System.Timers.Timer;

namespace Rumble.Platform.MailboxService.Services;

public class InboxService : PlatformMongoService<Inbox>
{
    public void DeleteExpired() // removes old expired messages in inboxes
    {
        //just keeping the ones that are not expired and are visible
        long deletionTime = Inbox.UnixTime + PlatformEnvironment.Optional<long?>("INBOX_DELETE_OLD_SECONDS") ?? 604800; // One week, in seconds
        
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
        
        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Expiration > deletionTime);
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.PullFilter(inbox => inbox.Messages, message => message.Expiration > deletionTime);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));

        _collection.BulkWrite(listWrites);
    }
    public InboxService() : base(collection: "inboxes") { }
    
    public override Inbox Get(string accountId)
    {
        return _collection.Find(filter: inbox => inbox.AccountId == accountId).FirstOrDefault();
    }

    public void UpdateAll(string id, Message edited)
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

    public void SendTo(string accountId, Message message)
    {
        // Inbox inbox = _collection.FindOneAndUpdate<Inbox>(
        //     filter: inbox => inbox.AccountId == accountId,
        //     update: Builders<Inbox>.Update.AddToSet(inbox => inbox.Messages, message),
        //     options: new FindOneAndUpdateOptions<Inbox>()
        //     {
        //         ReturnDocument = ReturnDocument.After
        //     }
        // );
        
        Message msg = message as Message;

        var result = _collection.UpdateOne<Inbox>(
            filter: inbox => inbox.AccountId == accountId,
            update: Builders<Inbox>.Update.AddToSet(inbox => inbox.Messages, msg)
        );
        

        return;
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
    
    public void BulkSend(IEnumerable<Message> messages)
    {
        foreach (Message message in messages)       // TODO: This needs to be optimized; this was done for rapid implementation
            _collection.UpdateOne<Inbox>(
                filter: inbox => inbox.AccountId == message.Recipient,
                update: Builders<Inbox>.Update.AddToSet(inbox => inbox.Messages, message)
            );
    }

    public void BulkSend(List<string> accountIds, List<Message> messages)
    {
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();

        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.In(inbox => inbox.AccountId, accountIds);
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.PushEach(inbox => inbox.Messages, messages);
        UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.PushEach(inbox => inbox.History, messages);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
        listWrites.Add(new UpdateManyModel<Inbox>(filter, updateHistory));
        _collection.BulkWrite(listWrites);
    }
}

// InboxService
// - On a timer, delete messages older than (Expiration + X), where X is a configurable amount of time from an environment variable.


