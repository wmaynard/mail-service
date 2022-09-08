using System.Collections.Generic;
using MongoDB.Driver;
using RCL.Logging;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class InboxService : PlatformMongoService<Inbox>
{
    // Removes old expired messages in inboxes
    public void DeleteExpired()
    {
        // just keeping the ones that are not expired and are visible

        long deletionTime = Timestamp.UnixTime;
        long deletionBuffer = (PlatformEnvironment.Optional<long?>("INBOX_DELETE_OLD_SECONDS") ?? 604800); // One week, in seconds
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
    public override Inbox Get(string accountId)
    {
        return _collection.Find(filter: inbox => inbox.AccountId == accountId).FirstOrDefault();
    }

    // Updates one message in an account's inbox
    public void UpdateOne(string id, string accountId, Message edited)
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

    // Updates the expiration on a message in an inbox
    public void UpdateExpiration(string id)
    {
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
        
        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.Messages, message => message.Id == id);
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Set(inbox => inbox.Messages[-1].Expiration, PlatformDataModel.UnixTime);
        FilterDefinition<Inbox> filterHistory = Builders<Inbox>.Filter.ElemMatch(inbox => inbox.History, message => message.Id == id);
        UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Set(inbox => inbox.History[-1].Expiration, PlatformDataModel.UnixTime);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
        listWrites.Add(new UpdateManyModel<Inbox>(filterHistory, updateHistory));
        _collection.BulkWrite(listWrites);
    }

    // Sends a message to multiple accounts
    public void SendTo(IEnumerable<string> accountIds, Message message)
    {
        List<WriteModel<Inbox>> listWrites = new List<WriteModel<Inbox>>();
        
        FilterDefinition<Inbox> filter = Builders<Inbox>.Filter.In(inbox => inbox.AccountId, accountIds);
        UpdateDefinition<Inbox> update = Builders<Inbox>.Update.Push(inbox => inbox.Messages, message);
        UpdateDefinition<Inbox> updateHistory = Builders<Inbox>.Update.Push(inbox => inbox.History, message);

        listWrites.Add(new UpdateManyModel<Inbox>(filter, update));
        listWrites.Add(new UpdateManyModel<Inbox>(filter, updateHistory));
        _collection.BulkWrite(listWrites);
    }
    
    // Sends multiple messages to the recipient
    public void BulkSend(IEnumerable<Message> messages)
    {
        foreach (Message message in messages)       // TODO: This needs to be optimized; this was done for rapid implementation
        {
            _collection.UpdateOne<Inbox>(
                                         filter: inbox => inbox.AccountId == message.Recipient,
                                         update: Builders<Inbox>.Update.AddToSet(inbox => inbox.Messages, message)
                                        );
        }
    }
}