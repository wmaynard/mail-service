using System.Collections.Generic;
using MongoDB.Driver;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class GlobalMessageService : PlatformMongoService<Message>
{
    public GlobalMessageService() : base(collection: "globalMessages") {  }

    public IEnumerable<Message> GetActiveGlobalMessages()
    {
        long timestamp = PlatformDataModel.UnixTime;
        return _collection.Find(filter:globalMessage => globalMessage.VisibleFrom < timestamp && globalMessage.Expiration > timestamp).ToList();
    }

    public IEnumerable<Message> GetAllGlobalMessages()
    {
        return _collection.Find(filter: globalMessage => true).ToList();
    }
}