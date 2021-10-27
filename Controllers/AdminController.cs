using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers
{
    [ApiController, Route(template: "mail/admin"), RequireAuth]
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
        public ObjectResult GlobalMessageSend()
        {
            GlobalMessage globalMessage = Require<GlobalMessage>(key: "globalMessage");
            _globalMessageService.Create(globalMessage);
            return Ok(globalMessage.ResponseObject); // response body contains the message sent
        }

        [HttpPatch, Route(template: "global/messages/edit"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageEdit() // TODO problem where globals in inboxes do not have their properties changed
        {
            string messageId = Require<string>(key: "messageId");
            GlobalMessage message = _globalMessageService.Get(messageId);
            
            string subject = Optional<string>(key: "subject") ?? message.Subject;
            string body = Optional<string>(key: "body") ?? message.Body;
            List<Attachment> attachments = Optional<List<Attachment>>(key: "attachments") ?? message.Attachments;
            long expiration = Optional<long?>(key: "expiration") ?? message.Expiration;
            long visibleFrom = Optional<long?>(key: "visibleFrom") ?? message.VisibleFrom;
            string image = Optional<string>(key: "image") ?? message.Image;
            Message.StatusType status = Optional<Message.StatusType?>(key: "statusType") ?? message.Status;
            Attachment attachment = Optional<Attachment>(key: "attachment") ?? message.Attachment;
            long? forAccountsBefore = Optional<long?>(key: "forAccountsBefore") ?? message.ForAccountsBefore;
            
            message.UpdateGlobal(subject: subject, body: body, attachments: attachments, expiration: expiration, visibleFrom: visibleFrom,
                image: image, status: status, attachment: attachment, forAccountsBefore: forAccountsBefore);
            
            _globalMessageService.Update(message);

            return Ok(message.ResponseObject);
        }

        [HttpPatch, Route(template: "global/messages/expire"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageExpire() // TODO problem where globals in inboxes do not have their expirations changed
        {
            string messageId = Require<string>(key: "messageId");

            GlobalMessage message = _globalMessageService.Get(messageId);
            message.ExpireGlobal(); // manually expires the message in question
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