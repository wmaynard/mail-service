using System.Collections.Generic;
using MongoDB.Driver;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services
{
    public class GlobalMessageService : PlatformMongoService<GlobalMessage>
    {
        public GlobalMessageService() : base(collection: "globalMessages") {  }

        public IEnumerable<GlobalMessage> GetActiveGlobalMessages()
        {
            long timestamp = GlobalMessage.UnixTime;
            return _collection.Find(filter:globalMessage => globalMessage.VisibleFrom < timestamp && globalMessage.Expiration > timestamp).ToList();
        }

        public IEnumerable<GlobalMessage> GetAllGlobalMessages()
        {
            return _collection.Find(filter: globalMessage => true).ToList();
        }
    }
}

// GlobalMessageService