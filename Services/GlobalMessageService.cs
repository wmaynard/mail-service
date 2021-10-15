using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services
{
    public class GlobalMessageService : PlatformMongoService<GlobalMessage>
    {
        public GlobalMessageService() : base(collection: "globalMessages") {  }

        public IEnumerable<GlobalMessage> GetAllGlobalMessages()
        {
            long timestamp = GlobalMessage.UnixTime;
            return GlobalMessage.Where(m => m.VisibleFrom < timestamp && m.Expiration > timestamp);
        }
    }
}

// GlobalMessageService