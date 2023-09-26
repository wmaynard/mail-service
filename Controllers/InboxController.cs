using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers;

[ApiController, Route(template: "mail/inbox"), RequireAuth]
public class InboxController : PlatformController
{
#pragma warning disable
    private readonly MinqInboxService _inboxService;
    private readonly GlobalMessageService _globalMessageService;
    private readonly MessageService _messageService;
#pragma warning restore

    [HttpGet, HealthMonitor(weight: 1)]
    public ActionResult GetInbox()
    {
        Inbox inbox = _inboxService.FromId(Token.AccountId);
        MailboxMessage[] eligible = _globalMessageService.GetEligibleMessages(inbox);
        
        _messageService.GrantGlobals(eligible);
        inbox.Messages = _messageService.GetUnexpiredMessages(Token.AccountId);
        
        return Ok(inbox);
    }

    [HttpPatch, Route("claim")]
    public ActionResult Claim2() => Ok(new RumbleJson
    {
        { "claimed", _messageService.Claim(Token.AccountId, Optional<string>("messageId"))}
    });
}