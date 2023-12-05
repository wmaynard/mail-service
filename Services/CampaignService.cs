using System;
using System.Linq;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class CampaignService : MinqService<MailboxMessage>
{
    public CampaignService() : base("campaigns")
    {
        mongo.DefineIndex(index => index.Add(message => message.PromoCode).EnforceUniqueConstraint());
    }
    
    public MailboxMessage FromClaimCode(string code) => mongo
        .FirstOrDefault(query => query
            .EqualTo(message => message.PromoCode, code)
            .Or(or => or
                .EqualTo(message => message.Expiration, 0)
                .GreaterThan(message => message.Expiration, Timestamp.Now)
            )
        )
        ?? throw new PlatformException("Campaign expired or unavailable", code: ErrorCode.MongoUnexpectedFoundCount);

    public MailboxMessage[] Define(MailboxMessage[] messages)
    {
        mongo.WithTransaction(out Transaction transaction);
        try
        {
            long deletedCount = mongo
                .WithTransaction(transaction)
                .Where(query => query.ContainedIn(db => db.PromoCode, messages.Select(message => message.PromoCode)))
                .Delete();
        
            mongo
                .WithTransaction(transaction)
                .Insert(messages);
        
            Commit(transaction);
        
            if (deletedCount > 0)
                Log.Info(Owner.Will, "Deleted email campaign messages with intent to replace", data: new
                {
                    Count = deletedCount
                });
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to define new campaigns.", exception: e);
            Abort(transaction);
        }
        
        return messages;
    }
}