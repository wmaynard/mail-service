using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers
{
    [ApiController, Route(template: "mail"), RequireAuth]
    public class TopController : PlatformController
    {
        private readonly InboxService _inboxService;
        private readonly GlobalMessageService _globalMessageService;
        public TopController(
            InboxService inboxService,
            GlobalMessageService globalMessageService,
            IConfiguration config) : base(config)
        {
            _inboxService = inboxService;
            _globalMessageService = globalMessageService;
        }

        [HttpGet, Route(template: "health"), NoAuth]
        public override ActionResult HealthCheck()
        {
            return Ok(
                _inboxService.HealthCheckResponseObject,
                _globalMessageService.HealthCheckResponseObject
            );
        }
    }
}

// All non-health endpoints should validate tokens for authorization.
// TopController
// - GET /mail/health