using System.Collections.Generic;
using MongoDB.Driver;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services
{
    public class GlobalMessageService : PlatformMongoService<GlobalMessage>
    {
        public GlobalMessageService() : base(collection: "globalMessages") {  }

        public IEnumerable<GlobalMessage> GetAllGlobalMessages()
        {
            long timestamp = GlobalMessage.UnixTime;
            return _collection.Find(filter:globalMessage => globalMessage.VisibleFrom < timestamp && globalMessage.Expiration > timestamp).ToList(); // maybe?
        }
    }
}

// GlobalMessageService