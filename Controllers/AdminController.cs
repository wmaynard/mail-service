using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers;

[ApiController, Route("mail/admin"), RequireAuth(AuthType.ADMIN_TOKEN)]
public class AdminController : PlatformController
{
#pragma warning disable
    private readonly InboxService _inboxes;
    private readonly GlobalMessageService _globalMessages;
    private readonly MessageService _messages;
    private readonly CampaignService _campaigns;
    private readonly GuidService _guids;
#pragma warning restore

    #region Direct Messages
    // Sends a new message to account(s)
    [HttpPost, Route("messages/send")]
    public ObjectResult MessageSend()
    {
        string[] accountIds = Require<string[]>("accountIds");
        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "message");

        _messages.Grant(mailboxMessage, accountIds);
        
        return Ok();
    }
    
    // Sends multiple messages to multiple accounts
    [HttpPost, Route("messages/send/bulk")]
    public ObjectResult BulkSend()
    {
        MailboxMessage[] messages = Require<MailboxMessage[]>("messages");
        
        Log.Info(Owner.Will, "Received request to grant a reward.", data: new
        {
            accountId = messages?.FirstOrDefault()?.Recipient,
            MailboxMessage = messages?.FirstOrDefault(),
            count = messages?.Length ?? 0
        }); 
        _messages.Insert(messages);

        return Ok(messages);
    }

    // Edits a message in a player's inbox
    [HttpPatch, Route("messages/edit")]
    public ObjectResult MessageEdit()
    {
        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "message");
        
        _messages.Update(mailboxMessage);
        
        return Ok(_inboxes.FromId(mailboxMessage.Recipient));
    }
    
    // Expires a message in a player's account
    [HttpPatch, Route("messages/expire")]
    public ObjectResult MessageExpire() => Ok(_messages.Expire(Require<string>("messageId")));
    
    #endregion
    
#region Global Messages
    // Fetches all global messages
    [HttpGet, Route("global/messages")]
    public ActionResult GlobalMessageList() => Ok(new RumbleJson
    {
        { "globalMessages", _globalMessages.Fetch(includeInactive: true) }
    });

    // Sends a new global message
    [HttpPost, Route("global/messages/send")]
    public ObjectResult GlobalMessageSend()
    {
        _globalMessages.Insert(Require<MailboxMessage>("globalMessage"));
        
        return Ok();
    }

    // Edits an existing global message
    [HttpPatch, Route("global/messages/edit")]
    public ObjectResult GlobalMessageEdit()
    {
        MailboxMessage mailboxMessage = Require<MailboxMessage>(key: "globalMessage");
        mailboxMessage.GlobalMessageId = mailboxMessage.Id;

        if (string.IsNullOrEmpty(mailboxMessage.Id))
            throw new PlatformException(message: "Global message update failed. A message id is required.");
        
        _globalMessages.Update(mailboxMessage);
        _messages.UpdateMany(mailboxMessage);
        
        return Ok(mailboxMessage);
    }

    // Expires an existing global message
    [HttpPatch, Route("global/messages/expire")]
    public ObjectResult ExpireGlobalMessage() => Ok(_globalMessages.Expire(Require<string>(key: "messageId")));
#endregion

    [HttpGet, Route("inbox")]
    public ObjectResult GetPlayerInbox()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        
        Inbox output = _inboxes.FromId(accountId);
        output.Messages = _messages.GetUnexpiredMessages(accountId);
            
        return Ok(output);
    }

    [HttpPost, Route("campaigns")]
    public ActionResult DefineEmailRewards()
    {
        CampaignMessage[] rewardEmails = Require<CampaignMessage[]>("campaigns");
        
        return Ok(new RumbleJson
        {
            { "campaigns", _campaigns.Define(rewardEmails) }
        });
    }

    [HttpPost, Route("promoPairings")]
    public ActionResult GeneratePromoPairs()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        long expiration = Require<long>("expiration");
        string[] promoCodes = Require<string[]>($"{MailboxMessage.FRIENDLY_KEY_PROMO_CODE}s");
        
        return Ok(new RumbleJson
        {
            { "guids", _guids.Generate(accountId, expiration, promoCodes) }
        });
    }

    [HttpPatch, Route("inboxAge")]
    public ActionResult SetAccountAge()
    {
        string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
        int days = Require<int>("days");

        return Ok(_inboxes.SetAccountAge(accountId, days));
    }
}