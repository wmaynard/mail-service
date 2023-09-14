using System.Collections.Generic;
using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class InboxService : PlatformMongoService<Inbox>
{
#pragma warning disable
    private readonly DynamicConfig _dynamicConfig;
#pragma warning restore
    
    // Removes old expired messages in inboxes
    public void DeleteExpired()
    {
        // just keeping the ones that are not expired and are visible

        long deletionTime = Timestamp.UnixTime;
        long deletionBuffer = (_dynamicConfig.Optional<long?>(key: "INBOX_DELETE_OLD_SECONDS") ?? 604800); // One week, in seconds
        UpdateResult result = _collection.UpdateMany(
            filter: Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Expiration < deletionTime - deletionBuffer),
            update: Builders<Inbox>.Update.PullFilter(inbox => inbox.Messages, messages => messages.Expiration < deletionTime - deletionBuffer)
        );
        if (result.ModifiedCount > 0)
        {
            Log.Info(Owner.Will, $"Deleted expired messages.", data: new
            { 
                AffectedAccounts = result.ModifiedCount
            });
        }
    }
    public InboxService() : base(collection: "inboxes") { }
    
    // Fetches an inbox using an accountId
    public override Inbox Get(string accountId) => _collection.Find(filter: inbox => inbox.AccountId == accountId).FirstOrDefault();

    // Updates one message in an account's inbox
    public void UpdateOne(string id, string accountId, MailboxMessage edited)
    {
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
        
        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.Eq(inbox => inbox.AccountId, accountId);
        filter &= Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Id == id);
        
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Set(inbox => inbox.Messages[-1], edited);
        
        FilterDefinition<Inbox> filterHistory = Builders<Inbox>.Filter.Eq(inbox => inbox.AccountId, accountId);
        filterHistory &= Builders<Inbox>.Filter.ElemMatch(inbox => inbox.History, message => message.Id == id);
        
        UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Set(inbox => inbox.History[-1], edited);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
        listWrites.Add(new UpdateManyModel<Inbox>(filterHistory, updateHistory));
        _collection.BulkWrite(listWrites);
    }

    // Updates all instances of a message in all inboxes
    public void UpdateAll(string id, MailboxMessage edited)
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

    // Updates the expiration on a message in an inbox
    public void UpdateExpiration(string id)
    {
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
        
        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Id == id);
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Set(inbox => inbox.Messages[-1].Expiration, Timestamp.UnixTime);
        FilterDefinition<Inbox> filterHistory = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.History, message => message.Id == id);
        UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Set(inbox => inbox.History[-1].Expiration, Timestamp.UnixTime);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
        listWrites.Add(new UpdateManyModel<Inbox>(filterHistory, updateHistory));
        _collection.BulkWrite(listWrites);
    }

    // Sends a message to multiple accounts
    public void SendTo(IEnumerable<string> accountIds, MailboxMessage mailboxMessage)
    {
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
        
        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.In(inbox => inbox.AccountId, accountIds);
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Push(inbox => inbox.Messages, mailboxMessage);
        UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Push(inbox => inbox.History, mailboxMessage);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
        listWrites.Add(new UpdateManyModel<Inbox>(filter, updateHistory));
        _collection.BulkWrite(listWrites);
    }
    
    // Sends multiple messages to the recipient
    public long BulkSend(IEnumerable<MailboxMessage> messages)
    {
        long affected = 0;
        foreach (MailboxMessage message in messages)       // TODO: This needs to be optimized; this was done for rapid implementation
        {
            _collection.UpdateOne(
                filter: Builders<Inbox>.Filter.Eq(inbox => inbox.AccountId, message.Recipient),
                update: Builders<Inbox>.Update.AddToSet(inbox => inbox.Messages, message)
            );
            affected++;
        }

        return affected;
    }
}

public class MinqInboxService : MinqService<Inbox>
{
    private long DELETION_BUFFER => _dynamicConfig.Optional<long?>(key: "INBOX_DELETE_OLD_SECONDS") ?? 604800; // One week, in seconds
    
#pragma warning disable
    private readonly DynamicConfig _dynamicConfig;
#pragma warning restore
    
    public MinqInboxService() : base("inboxes") { }

    public Inbox Get(string accountId) => mongo
        .Where(query => query.EqualTo(inbox => inbox.AccountId, accountId))
        .FirstOrDefault();

    public void DeleteExpired() => mongo
        .RemoveElements(
            model => model.Messages, 
            query => query.LessThanOrEqualTo(message => message.Expiration, Timestamp.UnixTime - DELETION_BUFFER)
        );

    public void UpdateOne(string messageId, string accountId, MailboxMessage edited)
    {
        mongo
            .Where(query => query.EqualTo(inbox => inbox.AccountId, accountId))
            .Update(query => query.UpdateItem())
    }
}



