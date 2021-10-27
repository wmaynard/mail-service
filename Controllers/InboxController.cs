using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers
{
    [ApiController, Route(template: "mail/inbox"), RequireAuth]
    public class InboxController : PlatformController
    {
        private readonly InboxService _inboxService;
        private readonly GlobalMessageService _globalMessageService;

        public InboxController(InboxService inboxService, GlobalMessageService globalMessageService, IConfiguration config) : base(config)
        {
            _inboxService = inboxService;
            _globalMessageService = globalMessageService;
        }

        [HttpGet, Route(template: "health"), NoAuth]
        public override ActionResult HealthCheck()
        {
            return Ok(_inboxService.HealthCheckResponseObject);
        }

        [HttpGet]
        public ObjectResult GetInbox() {
            Inbox accountInbox = _inboxService.Get(Token.AccountId);
            
            if (accountInbox == null) // means new account, need to call GetInbox() when account is created
            {
                GlobalMessage[] globalMessages = _globalMessageService.GetAllGlobalMessages()
                    .Where(message => message.ForAccountsBefore > Inbox.UnixTime || message.ForAccountsBefore == null)
                    .Where(message => !message.IsExpired)
                    .Select(message => message)
                    .OrderBy(message => message.Expiration)
                    .ToArray();
                accountInbox = new Inbox(aid: Token.AccountId, messages: new List<Message>());
                accountInbox.Messages.AddRange(globalMessages);
                _inboxService.Create(accountInbox);
                return Ok(accountInbox.ResponseObject); // returns inbox in question
            }

            // updating global messages
            GlobalMessage[] globals = _globalMessageService.GetAllGlobalMessages()
                // global message to avoid warning: Co-variant array conversion from GlobalMessage[] to Message[] can cause run-time exception on write operation
                // no warnings / errors elsewhere due to GetAllGlobalMessages() being changed to globalmessages, should be fine
                .Where(message => !accountInbox.Messages.Select(inboxMessage => inboxMessage.Id).Contains(message.Id))
                .Where(message => message.ForAccountsBefore > accountInbox.Timestamp || message.ForAccountsBefore == null)
                .Select(message => message)
                .ToArray();
            accountInbox.Messages.AddRange(globals);
            
            List<Message> filteredMessages = accountInbox.Messages
                .Where(message => !message.IsExpired)
                .Select(message => message)
                .OrderBy(message => message.Expiration)
                .ToList();
            
            accountInbox.UpdateMessages(filteredMessages); 

            _inboxService.Update(accountInbox);
            return Ok(accountInbox.ResponseObject); // returns inbox in question
        }

        [HttpPatch, Route(template: "claim")]
        public ObjectResult Claim()
        {
            string messageId = Require<string>(key: "messageId");
            Inbox accountInbox = _inboxService.Get(Token.AccountId);
            if (messageId == null) 
            {
                // claim all
                List<Message> messages = accountInbox.Messages;
                foreach (Message message in messages)
                {
                    message.UpdateClaimed();
                }
                _inboxService.Update(accountInbox);
            }
            else
            {
                // Message message = _globalMessageService.Get(messageId);
                // this would grab global message instead of message but globalmessage : message?
                // need a message service to do this more efficiently?
                // this implementation seems a little inefficient, TODO refactor
                Message message = null;
                int i = 0;
                while (i < accountInbox.Messages.Count() && message == null)
                {
                    if (accountInbox.Messages[i].Id == messageId)
                    {
                        message = accountInbox.Messages[i];
                    }

                    i++;
                }
                
                if (message == null)
                {
                    throw new Exception(message: "Claimed message was not found.");
                }
                else // linter says this is redundant, but this works on a compiler?
                {
                    message.UpdateClaimed();
                    _inboxService.Update(accountInbox);
                }
            }

            return Ok(accountInbox.ResponseObject); // maybe want to return the claimed attachments too TODO decision
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