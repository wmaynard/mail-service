using System.Collections.Generic;
using MongoDB.Driver;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services
{
    public class GlobalMessageService : PlatformMongoService<GlobalMessage>
    {
        public GlobalMessageService() : base(collection: "globalMessages") {  }

        public IEnumerable<Message> GetAllGlobalMessages() // this seems useful
        {
            long timestamp = GlobalMessage.UnixTime; // model to get UnixTime from platformcollectiondocument. is this strange?
            return _collection.Find(filter:globalMessage => globalMessage.VisibleFrom < timestamp && globalMessage.Expiration > timestamp).ToList(); // maybe?
        }
    }
}

// GlobalMessageService