using Rumble.Platform.Common.Services;

namespace Rumble.Platform.MailboxService.Services;

public class MessageExpirationService : PlatformTimerService
{
  private readonly InboxService _inboxService;
  
  public MessageExpirationService(InboxService inboxService) : base(intervalMS: 60_000, startImmediately: true)
  {
    _inboxService = inboxService;
  }

  protected override void OnElapsed()
  {
    _inboxService.DeleteExpired();
  }
}