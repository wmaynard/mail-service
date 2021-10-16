using System.Collections.Generic;
using System.Linq;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services
{
    public class GlobalMessageService : PlatformMongoService<GlobalMessage>
    {
        public GlobalMessageService() : base(collection: "globalMessages") {  }

        public IEnumerable<GlobalMessage> GetAllGlobalMessages() // this seems useful TODO building
        {
            long timestamp = GlobalMessage.UnixTime;
            return GlobalMessage.Where(m => m.VisibleFrom < timestamp && m.Expiration > timestamp); // need to make list of global messages instead to use .Where
        }
    }
}

// GlobalMessageService