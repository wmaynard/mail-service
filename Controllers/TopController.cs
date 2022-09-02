using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers;

[ApiController, Route(template: "mail")]
public class TopController : PlatformController
{
#pragma warning disable
    private readonly InboxService _inboxService;
    private readonly GlobalMessageService _globalMessageService;
#pragma warning restore
    
    // /health is handled by the base PlatformController and HealthService.
}