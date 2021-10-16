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
            IEnumerable<GlobalMessage> globalMessages = _globalMessageService.GetAllGlobalMessages(); // TODO implement

            return Ok(new {GlobalMessages = globalMessages}); // correct structure?
        }

        [HttpPost, Route(template: "messages/send"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult MessageSend() // TODO implement
        {
            List<string> accountIds = Require<List<string>>(key: "accountIds");
            // does body have the messages that are to be sent too?
        }

        [HttpPost, Route(template: "global/messages/send"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageSend() // TODO implement
        {
            bool eligibility = Require<bool>(key: "eligibleForNewAccounts");
            // does body have messages that are to be sent too?
        }

        [HttpPatch, Route(template: "global/messages/expire"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageExpire() // TODO implement
        {
            string messageId = Require<string>(key: "messageId");
            // this is probably called by the inboxservice timer?
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