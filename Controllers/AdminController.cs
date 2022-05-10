using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers;

[ApiController, Route(template: "mail/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
#pragma warning disable
    private readonly InboxService _inboxService;
    private readonly GlobalMessageService _globalMessageService;
#pragma warning restore


    [HttpGet, Route(template: "global/messages")]
    public ActionResult GlobalMessageList()
    {
        IEnumerable<Message> globalMessages = _globalMessageService.GetAllGlobalMessages();

        return Ok(new { GlobalMessages = globalMessages }); // just an object for now
    }

    [HttpPost, Route(template: "messages/send")]
    public ObjectResult MessageSend()
    {
        List<string> accountIds = Require<List<string>>(key: "accountIds");
        
        // TODO: Review below comment to see if it's still accurate
        // following modification needed because something in update to platform-common made it not pull values correctly for attachments
        // suspect it detects the keys nested inside the "attachment" key as separate keys, and defaults the quantity and type to 0 and null because it can't find a value

        Message message = Require<Message>(key: "message");
        message.Validate();

        try
        {
            _inboxService.SendTo(accountIds: accountIds, message: message);
        }
        catch (Exception)
        {
            Log.Error(owner: Owner.Nathan, message: "Message could not be sent to accountIds");
        }
        return Ok();
    }
    
    [HttpPost, Route(template: "messages/send/bulk")]
    public ObjectResult BulkSend()
    {
        Message[] messages = Require<Message[]>(key: "messages");

        try
        {
            if (messages.Any(message => string.IsNullOrEmpty(message.Recipient)))
                throw new PlatformException($"Missing key: '{Message.FRIENDLY_KEY_RECIPIENT}'.", code: ErrorCode.RequiredFieldMissing);
            _inboxService.BulkSend(messages);
        }
        catch (Exception e)
        {
            throw new PlatformException("Bulk messages could not be sent.", inner: e);
        }
        
        return Ok(new {Messages = messages});
    }

    [HttpPost, Route(template: "global/messages/send")]
    public ObjectResult GlobalMessageSend()
    {
        
        // following modification needed because something in update to platform-common made it not pull values correctly for attachments
        // suspect it detects the keys nested inside the "attachment" key as separate keys, and defaults the quantity and type to 0 and null because it can't find a value

        Message message = Require<Message>(key: "globalMessage");
        message.Validate();
        
        _globalMessageService.Create(message);
        
        return Ok();
    }

    [HttpPatch, Route(template: "global/messages/edit")]
    public ObjectResult GlobalMessageEdit()
    {
        // TODO: Refactor this endpoint
        // It looks like this endpoint is parsing every field that can be updated.  I would simplify this to make it more readable.
        // This can probably all be accomplished in just a couple of lines.  Ideally, it might look like:
        //
        // GlobalMessage updatedMessage = Require<GlobalMessage>(key: "message");
        //
        // if (string.isNullOrEmpty(updatedMessage.Id))
        //     throw new PlatformException(message: "Message updates require an ID.");
        //
        // _globalMessageService.Update(updatedMessage);
        //
        // This may have some side effects on how existing requests are structured.
        
        string messageId = Require<string>(key: "messageId");
        Message message = _globalMessageService.Get(messageId);
        
        if (message == null)
        {
            Log.Error(owner: Owner.Nathan, message: "Global message not found while attempting to edit", data: $"Global messageId: {messageId}");
            return Problem(detail: $"Global message {messageId} not found.");
        }

        Message copy = message.Copy(); // circular reference otherwise
        message.UpdatePrevious(copy);
        // incorrect format for following inputs should default to previous entry
        string subject = Optional<string>(key: "subject") ?? message.Subject;
        string body = Optional<string>(key: "body") ?? message.Body;
        // GenericData[] attachmentsData = Optional<GenericData[]>(key: "attachments");
        Attachment[] attachments = Optional<Attachment[]>(key: "attachments");
        // List<Attachment> attachments = message.Attachments;
        // if (attachmentsData != null)
        // {
        //     attachments = new List<Attachment>();
        //     foreach (GenericData ele in attachmentsData)
        //     {
        //         string attachmentString = ele.JSON;
        //         attachments.Add(JsonConvert.DeserializeObject<Attachment>(attachmentString));
        //     }
        // }
        long expiration = Optional<long?>(key: "expiration") ?? message.Expiration;
        long visibleFrom = Optional<long?>(key: "visibleFrom") ?? message.VisibleFrom;
        string icon = Optional<string>(key: "icon") ?? message.Icon;
        string banner = Optional<string>(key: "banner") ?? message.Banner;
        Message.StatusType status = Optional<Message.StatusType?>(key: "statusType") ?? message.Status;
        string internalNote = Optional<string>(key: "internalNote") ?? message.InternalNote;
        long? forAccountsBefore = Optional<long?>(key: "forAccountsBefore") ?? message.ForAccountsBefore;

        
        throw new NotImplementedException();
        // message.UpdateGlobal(subject: subject, body: body, attachments: attachments, expiration: expiration, visibleFrom: visibleFrom,
        //     icon: icon, banner: banner, status: status, internalNote: internalNote, forAccountsBefore: forAccountsBefore);

        try
        {
            message.Validate();
        }
        catch (Exception e)
        {
            Log.Error(owner: Owner.Nathan, message: "Editing global message failed.", data: e.Message);
            return Problem(detail: "Editing global message failed.");
        }
        
        _inboxService.UpdateAll(id: messageId, edited: message);
        _globalMessageService.Update(message);

        return Ok(message.ResponseObject);
    }

    [HttpPatch, Route(template: "global/messages/expire")]
    public ObjectResult GlobalMessageExpire()
    {
        string messageId = Require<string>(key: "messageId");
        Message message = _globalMessageService.Get(messageId);

        if (message == null)
        {
            Log.Error(owner: Owner.Nathan, message: "Global message not found while attempting to expire", data: $"Global messageId: {messageId}");
            return Problem(detail: $"Global message {messageId} was not found.");
        }

        Message copy = message.Copy(); // circular reference otherwise
        message.UpdatePrevious(copy);
    
        message.Expire();
    
        _inboxService.UpdateExpiration(id: messageId);
        _globalMessageService.Update(message);
        return Ok(message.ResponseObject);
    }

    [HttpPost, Route(template: "inbox")]
    public ObjectResult GetInboxAdmin()
    {
        string accountId = Require<string>(key: "accountId");
        Inbox accountInbox = _inboxService.Get(accountId);

        if (accountInbox == null)
        {
            return Problem(detail: "Account with accountId does not exist.");
        }
        
        // updating global messages
        Message[] globals = _globalMessageService.GetActiveGlobalMessages()
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
        
        List<Message> unexpiredMessages = accountInbox.Messages
            .Where(message => !message.IsExpired)
            .Select(message => message)
            .OrderBy(message => message.Expiration)
            .ToList();
        accountInbox.UpdateMessages(unexpiredMessages);

        _inboxService.Update(accountInbox);

        List<Message> filteredMessages = accountInbox.Messages
            .Where(message => message.VisibleFrom < Inbox.UnixTime)
            .Select(message => message)
            .ToList();
        
        Inbox filteredInbox = new Inbox(aid: accountInbox.AccountId, messages: filteredMessages, history: accountInbox.History, timestamp: accountInbox.Timestamp, id: accountInbox.Id);
        
        return Ok(filteredInbox.ResponseObject);
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
// - POST /mail/admin/inbox
//   - body should contain an accountId