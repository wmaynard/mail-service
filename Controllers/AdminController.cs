using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers;

[ApiController, Route(template: "mail/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
#pragma warning disable
    private readonly InboxService _inboxService;
    private readonly GlobalMessageService _globalMessageService;
    private readonly MessageService _messageService;
#pragma warning restore

    #region Direct Messages
    // Sends a new message to account(s)
    [HttpPost, Route(template: "messages/send")]
    public ObjectResult MessageSend()
    {
        string[] accountIds = Require<string[]>(key: "accountIds");
        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "message");

        _messageService.Grant(mailboxMessage, accountIds);
        
        return Ok();
    }
    
    // Sends multiple messages to multiple accounts
    [HttpPost, Route(template: "messages/send/bulk")]
    public ObjectResult BulkSend()
    {
        MailboxMessage[] messages = Require<MailboxMessage[]>(key: "messages");
        
        Log.Info(Owner.Will, "Received request to grant a reward.", data: new
        {
            accountId = messages?.FirstOrDefault()?.Recipient,
            MailboxMessage = messages?.FirstOrDefault(),
            count = messages?.Length ?? 0
        }); 
        _messageService.Insert(messages);

        return Ok(messages);
    }

    // Edits a message in a player's inbox
    [HttpPatch, Route(template: "messages/edit")]
    public ObjectResult MessageEdit()
    {
        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "message");
        
        _messageService.Replace(mailboxMessage);

        return Ok(_inboxService.Get(mailboxMessage.Recipient));
    }
    
    // Expires a message in a player's account
    [HttpPatch, Route(template: "messages/expire")]
    public ObjectResult MessageExpire() => Ok(_messageService.Expire(Require<string>("messageId")));
    
    #endregion
    
    #region Global Messages
    // Fetches all global messages
    [HttpGet, Route(template: "global/messages")]
    public ActionResult GlobalMessageList() => Ok(new RumbleJson
    {
        {"globalMessages", _globalMessageService.GetAllGlobalMessages()}
    });

    // Sends a new global message
    [HttpPost, Route(template: "global/messages/send")]
    public ObjectResult GlobalMessageSend()
    {
        _globalMessageService.Create(Require<MailboxMessage>(key: "globalMessage"));
        
        return Ok();
    }
    
    /////////////////////////////////
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// 

    // Edits an existing global message
    [HttpPatch, Route(template: "global/messages/edit")]
    public ObjectResult GlobalMessageEdit()
    {

        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "globalMessage");

        if (string.IsNullOrEmpty(mailboxMessage.Id))
        {
            throw new PlatformException(message: "Global message update failed. A message id is required.");
        }
        
        mailboxMessage.Validate();

        MailboxMessage oldMailboxMessage = _globalMessageService.Get(mailboxMessage.Id);
        
        if (oldMailboxMessage == null)
            throw new PlatformException(message: $"Global message to edit not found.");
        
        mailboxMessage.UpdatePrevious(oldMailboxMessage);
        
        try
        {
            mailboxMessage.Validate();
        }
        catch (Exception e)
        {
            throw new PlatformException(message: "Editing global message failed.", inner: e);
        }
        
        _inboxService.UpdateAll(id: mailboxMessage.Id, edited: mailboxMessage);
        _globalMessageService.Update(mailboxMessage);

        return Ok(mailboxMessage.ResponseObject);
    }

    // Expires an existing global message
    [HttpPatch, Route(template: "global/messages/expire")]
    public ObjectResult GlobalMessageExpire()
    {
        string messageId = Require<string>(key: "messageId");
        MailboxMessage mailboxMessage = _globalMessageService.Get(messageId);

        if (mailboxMessage == null)
            throw new PlatformException(message: $"Global message to expire was not found.");

        MailboxMessage copy = mailboxMessage.Copy(); // circular reference otherwise
        mailboxMessage.UpdatePrevious(copy);
    
        mailboxMessage.Expire();
    
        _inboxService.UpdateExpiration(id: messageId);
        _globalMessageService.Update(mailboxMessage);
        return Ok(mailboxMessage.ResponseObject);
    }
    #endregion

    #region Player Inbox
    // Fetches a player's inbox without needing their token
    [HttpGet, Route(template: "inbox")]
    public ObjectResult GetInboxAdmin()
    {
        string accountId = Require<string>(key: "accountId");
        Inbox accountInbox = _inboxService.Get(accountId);

        if (accountInbox == null)
        {
            throw new PlatformException(message: $"Inbox with accountId not found.");
        }
        
        // updating global messages
        MailboxMessage[] globals = _globalMessageService.GetActiveGlobalMessages()
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
        catch (Exception e)
        {
            Log.Error(owner: Owner.Nathan, message: "Error while trying to add globals to account. Inbox may be malformed.", exception: e);
        }
        
        List<MailboxMessage> unexpiredMessages = accountInbox.Messages
            .Where(message => !message.IsExpired)
            .Select(message => message)
            .OrderBy(message => message.Expiration)
            .ToList();
        foreach (MailboxMessage message in unexpiredMessages)
        {
            message.Validate();
        }
        accountInbox.Messages = unexpiredMessages;

        _inboxService.Update(accountInbox);

        List<MailboxMessage> filteredMessages = accountInbox.Messages
            .Where(message => message.VisibleFrom < Timestamp.UnixTime)
            .Select(message => message)
            .ToList();
        
        Inbox filteredInbox = new Inbox(aid: accountInbox.AccountId, messages: filteredMessages, history: accountInbox.History, timestamp: accountInbox.Timestamp, id: accountInbox.Id);
        
        return Ok(filteredInbox.ResponseObject);
    }
    #endregion
}