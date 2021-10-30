using System;
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
            try
            {
                _inboxService.SendTo(accountIds: accountIds, message: message);
            }
            catch (Exception)
            {
                Log.Error(owner: Owner.Nathan, message: $"Message {message} could not be sent to accounts {accountIds}.");
            }
            return Ok(message.ResponseObject);
        }

        [HttpPost, Route(template: "global/messages/send"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageSend()
        {
            GlobalMessage globalMessage = Require<GlobalMessage>(key: "globalMessage");
            _globalMessageService.Create(globalMessage);
            return Ok(globalMessage.ResponseObject);
        }

        [HttpPatch, Route(template: "global/messages/edit"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageEdit()
        {
            string messageId = Require<string>(key: "messageId");
            GlobalMessage message = _globalMessageService.Get(messageId);
            
            if (message == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Global message {messageId} not found while attempting to edit.");
                return Problem(detail: $"Global message {messageId} not found.");
            }
            
            GlobalMessage copy = GlobalMessage.CreateCopy(message); // circular reference otherwise
            message.UpdatePrevious(copy);
            // incorrect format for following inputs should default to previous entry
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
            
            _inboxService.UpdateAll(id: messageId, edited: message);
            _globalMessageService.Update(message);

            return Ok(message.ResponseObject);
        }

        [HttpPatch, Route(template: "global/messages/expire"), RequireAuth(TokenType.ADMIN)]
        public ObjectResult GlobalMessageExpire()
        {
            string messageId = Require<string>(key: "messageId");
            GlobalMessage message = _globalMessageService.Get(messageId);

            if (message == null)
            {
                Log.Error(owner: Owner.Nathan, message: $"Global message {messageId} not found while attempting to expire.");
                return Problem(detail: $"Global message {messageId} was not found.");
            }

            GlobalMessage copy = GlobalMessage.CreateCopy(message); // circular reference otherwise
            message.UpdatePrevious(copy);
        
            message.ExpireGlobal();
        
            _inboxService.UpdateExpiration(id: messageId);
            _globalMessageService.Update(message);
            return Ok(message.ResponseObject);
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
// - PATCH /mail/admin/global/messages/edit
//   - body should contain a messageId and all parameters, incorrect parameter types are ignored
// - PATCH /mail/admin/global/messages/expire
//   - body should contain a messageId