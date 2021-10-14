using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService.Models
{
    public class Message
    {
        
    }
}

// Message
// - Subject
// - Body
// - Collection of Attachments
// - Timestamp (Unix timestamp, assigned on creation)
// - Expiration (Unix timestamp)
// - VisibleFrom (Unix timestamp)
// - Image (string value)
// - Status (CLAIMED or UNCLAIMED)
// - IsExpired (getter property)