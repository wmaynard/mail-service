using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class GuidService : MinqTimerService<GuidPairing>
{
    public GuidService() : base("promoGuids") { }

    public GuidPairing[] Generate(string accountId, long expiration, string[] promoCodes)
    {
        GuidPairing[] pairings = promoCodes.Select(code => new GuidPairing
        {
            AccountId = accountId,
            Expiration = expiration,
            PromoCode = code
        }).ToArray();

        mongo.WithTransaction(out Transaction transaction);
        try
        {
            long affected = mongo
                .WithTransaction(transaction)
                .Where(query => query
                    .EqualTo(pairing => pairing.AccountId, accountId)
                    .ContainedIn(pairing => pairing.PromoCode, promoCodes)
                ).Delete();
        
            if (affected > 0)
                Log.Info(Owner.Will, "Deleted pre-existing promo code guid pairings");
        
            mongo
                .WithTransaction(transaction)
                .Insert(pairings);
            Commit(transaction);
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to update player promo code pairings", data: new
            {
                codes = promoCodes
            }, exception: e);
            Abort(transaction);
        }

        return pairings;
    }
    
    public GuidPairing EnforceValidCode(string id) => mongo
        .ExactId(id)
        .And(query => query.GreaterThanOrEqualTo(pairing => pairing.Expiration, Timestamp.Now))
        .FirstOrDefault()
        ?? throw new PlatformException("Code not found or expired.");

    protected override void OnElapsed()
    {
        mongo
            .Where(query => query.LessThanOrEqualTo(pairing => pairing.Expiration, Timestamp.Now))
            .Delete();
    }

    public void Delete(GuidPairing pairing) => mongo
        .ExactId(pairing.Id)
        .Delete();
}

public class GuidPairing : PlatformCollectionDocument
{
    [BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID)]
    public string AccountId { get; set; }
    
    [BsonElement(MailboxMessage.DB_KEY_PROMO_CODE)]
    public string PromoCode { get; set; }
    
    [BsonElement("exp")]
    public long Expiration { get; set; }
}