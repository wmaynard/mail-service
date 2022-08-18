using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.MailboxService;

public class Startup : PlatformStartup
{
    protected override PlatformOptions Configure(PlatformOptions options) => options
        .SetProjectOwner(Owner.Nathan)
        .SetRegistrationName("Mail")
        .SetPerformanceThresholds(warnMS: 500, errorMS: 2_000, criticalMS: 30_000)
        .DisableServices(CommonService.Config);
}