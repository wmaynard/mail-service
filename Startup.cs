using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService;

public class Startup : PlatformStartup
{
    protected override PlatformOptions ConfigureOptions(PlatformOptions options) => options
        .SetProjectOwner(Owner.Nathan)
        .SetRegistrationName("Mail")
        .SetPerformanceThresholds(warnMS: 500, errorMS: 2_000, criticalMS: 30_000)
        .DisableServices(CommonService.Config);
}