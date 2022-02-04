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
                GlobalMessage[] globalMessages = _globalMessageService.GetActiveGlobalMessages()
                    .Where(message => message.ForAccountsBefore > Inbox.UnixTime || message.ForAccountsBefore == null)
                    .Where(message => !message.IsExpired)
                    .Select(message => message)
                    .OrderBy(message => message.Expiration)
                    .ToArray();
                accountInbox = new Inbox(aid: Token.AccountId, messages: new List<Message>(), history: new List<Message>());
                accountInbox.Messages.AddRange(globalMessages);
                accountInbox.History.AddRange(globalMessages);
                _inboxService.Create(accountInbox);
                return Ok(accountInbox.ResponseObject);
            }

            // updating global messages
            GlobalMessage[] globals = _globalMessageService.GetActiveGlobalMessages()
                .Where(message => !(accountInbox.Messages.Select(inboxMessage => inboxMessage.Id).Contains(message.Id)))
                .Where(message => !message.IsExpired)
                .Where(message => message.ForAccountsBefore > accountInbox.Timestamp || message.ForAccountsBefore == null)
                .Select(message => message)
                .ToArray();
            try
            {
                accountInbox.Messages.AddRange(globals);
                if (accountInbox.History == null)
                {
                    accountInbox.CreateHistory();
                }
                accountInbox.History.AddRange(globals);
            }
            catch (Exception)
            {
                Log.Error(owner: Owner.Nathan, message: "Error while trying to add globals to account. Inbox may be malformed.", data: $"AccountId: {Token.AccountId}");
            }
            
            List<Message> filteredMessages = accountInbox.Messages
                .Where(message => !message.IsExpired)
                .Select(message => message)
                .OrderBy(message => message.Expiration)
                .ToList();
            accountInbox.UpdateMessages(filteredMessages);

            _inboxService.Update(accountInbox);
            return Ok(accountInbox.ResponseObject);
        }

        [HttpPatch, Route(template: "claim")]
        public ObjectResult Claim()
        {
            string messageId = Optional<string>(key: "messageId");
            Inbox accountInbox = _inboxService.Get(Token.AccountId);
            List<Message> claimed = new List<Message>();
            if (messageId == null)
            {
                // claim all
                List<Message> messages = accountInbox.Messages;
                foreach (Message message in messages)
                {
                    if (message.Status == 0)
                    {
                        try
                        {
                            message.UpdateClaimed();
                            claimed.Add(message);
                            Message record = accountInbox.History.Find(history => history.Id == message.Id);
                            try
                            {
                                record.UpdateClaimed();
                            }
                            catch (Exception e)
                            {
                                Log.Error(owner: Owner.Nathan, message: "Error occurred while updating history for claimed message.", data: $"AccountId {accountInbox.AccountId}, message: {message}. {e.Message}");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(owner: Owner.Nathan, message: "Error occured while claiming all messages.", data: $"{e.Message}");
                        }
                    }

                    _inboxService.Update(accountInbox);
                }
            }
            else
            {
                // claim one
                Message message = accountInbox.Messages.Find(message => message.Id == messageId);
                try
                {
                    if (message == null)
                    {
                        throw new Exception(message: $"Message {messageId} was not found while attempting to claim for accountId {Token.AccountId}.)");
                    }
                    message.UpdateClaimed();
                    Message record = accountInbox.History.Find(history => history.Id == message.Id);
                    try
                    {
                        record.UpdateClaimed();
                    }
                    catch (Exception e)
                    {
                        Log.Error(owner: Owner.Nathan, message: "Error occurred while updating history for claimed message.", data: $"AccountId {accountInbox.AccountId}, message: {message}. {e.Message}");
                    }
                    claimed.Add(message);
                    _inboxService.Update(accountInbox);
                }
                catch (Exception e)
                {
                    Log.Error(owner: Owner.Nathan, message: "Error occured while trying to claim a message", data: $"{e.Message}");
                    return Problem(e.Message);
                }
            }
            return Ok(new {claimed = claimed});
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