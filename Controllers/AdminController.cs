using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
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
        
        _messageService.Update(mailboxMessage);
        
        return Ok(_inboxService.FromId(mailboxMessage.Recipient));
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
        { "globalMessages", _globalMessageService.Fetch(includeInactive: true) }
    });

    // Sends a new global message
    [HttpPost, Route(template: "global/messages/send")]
    public ObjectResult GlobalMessageSend()
    {
        _globalMessageService.Insert(Require<MailboxMessage>("globalMessage"));
        
        return Ok();
    }

    // Edits an existing global message
    [HttpPatch, Route(template: "global/messages/edit")]
    public ObjectResult GlobalMessageEdit()
    {
        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "globalMessage");
        mailboxMessage.GlobalMessageId = mailboxMessage.Id;

        if (string.IsNullOrEmpty(mailboxMessage.Id))
            throw new PlatformException(message: "Global message update failed. A message id is required.");
        
        _globalMessageService.Update(mailboxMessage);
        _messageService.UpdateMany(mailboxMessage);
        
        return Ok(mailboxMessage);
    }

    // Expires an existing global message
    [HttpPatch, Route(template: "global/messages/expire")]
    public ObjectResult ExpireGlobalMessage() => Ok(_globalMessageService.Expire(Require<string>(key: "messageId")));
#endregion

    [HttpGet, Route(template: "inbox")]
    public ObjectResult GetPlayerInbox()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        
        Inbox output = _inboxService.FromId(accountId);
        output.Messages = _messageService.GetUnexpiredMessages(accountId);
            
        return Ok(output);
    }
}