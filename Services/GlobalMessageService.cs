using System.Linq;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.MailboxService.Models;

namespace Rumble.Platform.MailboxService.Services;

public class GlobalMessageService : MinqService<MailboxMessage>
{
    public GlobalMessageService() : base(collection: "globalMessages") {  }
    
    public MailboxMessage[] Fetch(bool includeInactive = false) => includeInactive
        ? mongo.All().ToArray()
        : mongo
            .Where(query => query
                .LessThan(message => message.VisibleFrom, Timestamp.Now)
                .GreaterThan(message => message.Expiration, Timestamp.Now)
            )
            .ToArray();

    public MailboxMessage[] GetEligibleMessages(Inbox inbox) => mongo
        .Where(query => query
            .LessThan(message => message.VisibleFrom, Timestamp.Now)
            .GreaterThan(message => message.Expiration, Timestamp.Now)
            .Or(or => or
                .GreaterThan(message => message.ForAccountsBefore, inbox.CreatedOn)
                .EqualTo(message => message.ForAccountsBefore, 0)
                .FieldDoesNotExist(message => message.ForAccountsBefore)
            )
        )
        .Sort(sort => sort.OrderByDescending(message => message.Expiration))
        .ToArray()
        .Select(message => // Assigning the account ID here helps guarantee we don't insert a global message without a recipient later
        {
            message.Recipient = inbox.AccountId;
            return message;
        })
        .ToArray();
    
    public MailboxMessage Get(string id) => FromId(id)
        ?? throw new PlatformException(message: "Global message to expire was not found.");

    public MailboxMessage Expire(string id) => mongo
        .ExactId(id)
        .Upsert(query => query.SetToCurrentTimestamp(message => message.Expiration));


    public override MailboxMessage FromId(string id)
    {
        MailboxMessage output = base.FromId(id)
            ?? throw new PlatformException(message: $"Global message not found.");
        
        output.GlobalMessageId = output.GlobalMessageId = output.Id;
        return output;
    }
}