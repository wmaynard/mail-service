using Microsoft.AspNetCore.Mvc;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Web;
using Rumble.Platform.MailboxService.Models;
using Rumble.Platform.MailboxService.Services;

namespace Rumble.Platform.MailboxService.Controllers;

[ApiController, Route("mail")]
public class TopController : PlatformController
{
    #pragma warning disable
    private readonly CampaignService _campaigns;
    private readonly MessageService _messages;
    private readonly GuidService _guids;
    private readonly InboxService _inboxes;
    #pragma warning restore

    [HttpGet, Route("claim"), NoAuth]
    public ActionResult ClaimReward()
    {
        string promoCode = Require<string>(MailboxMessage.FRIENDLY_KEY_PROMO_CODE);
        string accountId;
        GuidPairing pairing = null;
        
        if (DynamicConfig.Optional<bool>("useGuidCampaignFormat", defaultValue: false))
        {
            pairing = _guids.EnforceValidCode(promoCode);
            promoCode = pairing.PromoCode;
            accountId = pairing.AccountId;
        }
        else
        {
            accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
            
            if (!accountId.CanBeMongoId())
                throw new PlatformException("Invalid account ID", code: ErrorCode.InvalidRequestData);
        }
        
        MailboxMessage reward = _campaigns.FromClaimCode(promoCode);
        reward.ChangeId();
        
        _inboxes.EnforceAccountAgeOver(accountId, reward.MinimumAccountAge);
        _messages.EnforcePromoUnclaimed(accountId, promoCode);

        reward.Recipient = accountId;
        _messages.Insert(reward);
        if (pairing != null)
            _guids.Delete(pairing);

        return Ok(reward);
    }
}