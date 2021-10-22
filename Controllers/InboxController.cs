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
    [ApiController, Route(template: "inbox"), RequireAuth]
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
        public ObjectResult GetInbox()
        {
            IEnumerable<Message> globalMessages = _globalMessageService.GetAllGlobalMessages();
            Inbox accountInbox = _inboxService.Get(Token.AccountId);
            if (accountInbox == null)
            {
                accountInbox = new Inbox(aid: Token.AccountId, messages: new List<Message>());
                _inboxService.Create(accountInbox);
                accountInbox.UpdateMessages(globalMessages.ToList());
            }
            else
            {
                // optimizations could be made here for an algorithm to combine based on what ids are not present in inbox TODO
                // how to access message ids? message.Id
                // -> plan - make an object for the ids of the globalmessages, iterate once through inbox.messages, add missing ones after - O(n + m)
                // plan - make an object for the ids of the inbox.messages, iterate once through globalmessages, add if missing - O(n + m)
                // plan - if both are sorted, iterate through and merge - O(n + m) but save a little on memory
                // plan - Enumerable.Union? with a comparer for id - O(?)
                
                HashSet<string> globalMessageIds = new HashSet<string>(); // hashset for constant lookup
                
                foreach (Message globalMessage in globalMessages) // populating hashset with all global message ids
                {
                    globalMessageIds.Add(globalMessage.Id);
                }

                foreach (Message inboxMessage in accountInbox.Messages) // getting rid of duplicate global message ids
                {
                    if (globalMessageIds.Contains(inboxMessage.Id))
                    {
                        globalMessageIds.Remove(inboxMessage.Id);
                    }
                }

                foreach (string globalMessageId in globalMessageIds) // adding in new global messages into existing messages list
                {
                    accountInbox.Messages.Add(item:_globalMessageService.Get(globalMessageId));
                }
                _inboxService.Update(accountInbox);
            }
            return Ok(accountInbox.ResponseObject); // returns inbox in question
        }

        [HttpPatch, Route(template: "claim")]
        public ObjectResult Claim() // TODO implement
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
                _inboxService.Update(accountInbox); // update only once? or for each message update
            }
            else
            {
                // message has an id from service.Get()
                // need to work with inbox.messages and claim that specific one TODO fix
                Message message = _globalMessageService.Get(messageId); 
                // this currently grabs global message instead of message? but globalmessage : message TODO fix
                message.UpdateClaimed();
                _inboxService.Update(accountInbox);
            }

            return Ok(accountInbox.ResponseObject); // response body is the resulting inbox
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