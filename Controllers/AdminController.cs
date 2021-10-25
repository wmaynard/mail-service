using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers
{
    [ApiController, Route(template: "admin"), RequireAuth]
    public class AdminController : PlatformController
    {
        private readonly InboxService _inboxService;
        private readonly GlobalMessageService _globalMessageService;

        public AdminController(InboxService inboxService, GlobalMessageService globalMessageService, IConfiguration config) : base(config)
        {
            _inboxService = inboxService;
            _globalMessageService = globalMessageService;
        }

        [HttpGet, Route(template: "health"), NoAuth]
        public override ActionResult HealthCheck()
        {
            return Ok(_inboxService.HealthCheckResponseObject, _globalMessageService.HealthCheckResponseObject);
        }

        [HttpGet, Route(template: "global/messages"), RequireAuth(TokenType.ADMIN)]
        public ActionResult GlobalMessageList()
        {
            IEnumerable<Message> globalMessages = _globalMessageService.GetAllGlobalMessages();

            return Ok(new {GlobalMessages = globalMessages}); // just an object for now
        }

        [HttpPost, Route(template: "messages/send"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult MessageSend()
        {
            List<string> accountIds = Require<List<string>>(key: "accountIds");
            Message message = Require<Message>(key: "message");
            // need to add the message in inbox for each accountId
            foreach (string accountId in accountIds) // possibly refactor to be more efficient TODO refactor
            {
                Inbox inbox = _inboxService.Get(accountId);
                inbox.Messages.Add(message);
                _inboxService.Update(inbox);
            }
            
            return Ok(message.ResponseObject); // response body contains the message sent
        }

        [HttpPost, Route(template: "global/messages/send"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageSend() // TODO check
        {
            bool eligibleNew = Require<bool>(key: "eligibleForNewAccounts");
            GlobalMessage globalMessage = Require<GlobalMessage>(key: "globalMessage");
            // need to add the globalmessage in inbox for all accountids, eligibility included
            if (eligibleNew) // put global message in pool to be fetched by anyone
            {
                _globalMessageService.Create(globalMessage);
            }
            else // add to existing accounts only
            {
                IEnumerable<Inbox> allInboxes = _inboxService.List();
                foreach (Inbox inbox in allInboxes)
                {
                    inbox.Messages.Add(globalMessage);
                    _inboxService.Update(inbox);
                }
            }
            return Ok(globalMessage.ResponseObject); // response body contains the message sent
        }

        [HttpPatch, Route(template: "global/messages/expire"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageExpire()
        {
            string messageId = Require<string>(key: "messageId");

            GlobalMessage message = _globalMessageService.Get(messageId);
            message.Expire(); // manually expires the message in question
            _globalMessageService.Update(message);

            return Ok(message.ResponseObject); // response body contains the message expired
        }
    }
}

// All non-health endpoints should validate tokens for authorization.
// Any non-health admin endpoint should also check that tokens belong to admins.
// AdminController
// - GET /mail/admin/health
// - GET /mail/admin/global/messages
//   - returns all global messages, sorted by expiration ascending
// - POST /mail/admin/messages/send
//   - body should contain an array of accountIds
// - POST /mail/admin/global/messages/send
//   - body should contain a bool for eligibleForNewAccounts
// - PATCH /mail/admin/global/messages/expire
//   - body should contain a messageId