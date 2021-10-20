using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers
{
    [ApiController, Route(template: "inbox"), RequireAuth]
    public class InboxController : PlatformController
    {
        private readonly InboxService _inboxService;

        public InboxController(InboxService inboxService, IConfiguration config) : base(config)
        {
            _inboxService = inboxService;
        }

        [HttpGet, Route(template: "health"), NoAuth]
        public override ActionResult HealthCheck()
        {
            return Ok(_inboxService.HealthCheckResponseObject);
        }

        [HttpGet]
        public ObjectResult GetInbox()
        {
            Inbox accountInbox = _inboxService.Get(Token.AccountId); // check null, global messages TODO
            return Ok(accountInbox.ResponseObject);
        }

        [HttpPatch, Route(template: "claim")]
        public ObjectResult Claim() // TODO implement
        {
            string messageId = Require<string>(key: "messageId");
            Inbox accountInbox = _inboxService.Get(Token.AccountId);
            if (messageId == null) 
            { // maybe shouldn't do the logic/work here, but in inboxservice instead? TODO
                // claim all
                List<Message> messages = accountInbox.Messages;
                foreach (Message message in messages)
                {
                    message.UpdateClaimed();
                    _inboxService.Update(accountInbox); // want to try to force update immediately on messages
                }
            }
            else
            {
                // message has an id? primary key? enum in inbox.messages? TODO
                
            }

            return Ok(accountInbox.ResponseObject); // response body is the resulting inbox?
        }
    }
}

// All non-health endpoints should validate tokens for authorization.
// InboxController
// - GET /mail/inbox/health
// - GET /mail/inbox
//   - If the inbox is null, create one with any active eligible global messages in it.
//   - If inbox is not null and there are new global messages, update the inbox appropriately.
//   - Returns an Inbox with Messages sorted by expiration ascending
//   - Omits expired messages
// - PATCH /mail/inbox/claim
//   - body should contain a messageId to claim.  If null, claim all messages instead.
//   - Verify that each claimed message belongs to the accountId before anything is updated