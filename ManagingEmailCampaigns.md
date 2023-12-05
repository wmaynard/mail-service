# Managing Email Campaigns

Mailbox now supports the ability to grant players rewards based on engagement with marketing emails.  The goal is to send our subscribers messages that allow them to get rewards, driving engagement in the game.  There are two approaches to this; one is insecure but fast to roll out; the other is much more secure but requires more work to integrate with marketing email systems.  Both approaches require some basic steps.

## Step 1: Defining Message Rewards

Before we can send rewards out to players, we have to define what the messages will look like when delivered to players.  We do that by defining **campaigns.**  To do this, we need three new fields:

* `claimCode`, which is used as a unique identifier for the message
* `redirectUrl`, the URL to send players to after they've **successfully** claimed their rewards
  * The failure redirect is stored separately in Dynamic Config under DMZ, as `mailClaimFailurePage`.  When a claim fails, DMZ will not have access to information beyond an error code, and especially when claiming promo codes is concerned it may be desirable to leave a generic error message for players, such as "Ineligible or reward expired", and just leave it at that.  We will have logs showing more detail if needed.
* `minimumAgeInSeconds`, the minimum age an account has to be in order to collect the specific campaign rewards


```
POST /mail/admin/campaigns
{
    "campaigns": [
        {
            "subject": "Hello, World!",
            "body": "This is a test of the email campaign rewards system",
            "attachments": [
                {
                    "type": "currency",
                    "rewardId": "hard_currency",
                    "quantity": 100
                }
            ],
            "data": {},
            "icon": "",
            "banner": "",
            "internalNote": "testCampaign",
            "claimCode": "test_2",                         // <--
            "redirectUrl": "https://towersandtitans.com/", // <-- 
            "minimumAgeInSeconds": 86400                   // <--
        }
    ]
}

HTTP 200
{
    // Returns a copy of the data sent to it, but not previously-existing / unmodified campaigns.
    "campaigns": [
        {
            "subject": "Hello, World!",
            "body": "This is a test of the email campaign rewards system",
            "attachments": [
                {
                    "type": "currency",
                    "rewardId": "hard_currency",
                    "quantity": 100
                }
            ],
            "data": {},
            "timestamp": 1701808200,
            "expiration": 2648579400,
            "visibleFrom": 0,
            "icon": "",
            "banner": "",
            "status": 0,
            "internalNote": "testCampaign",
            "minimumAgeInSeconds": 86400,
            "claimCode": "test_2",
            "redirectUrl": "https://towersandtitans.com/",
            "id": "656f8848c352522d76b18334",
            "createdOn": 1701808201
        }
    ]
}
```

A couple of notes:

* If `expiration` is left blank or 0, it will be set to 30 years from the current time.  While mail-service does allow expirations, keep in mind that if the mail is expired when someone tries to collect it, they will see a failure screen.
* `id` & `createdOn` are assigned and returned, but these aren't necessary for any purpose beyond data queries on the Platform side.  Do not specify an `id` when submitting campaigns.
* Pre-existing campaigns with identical claim codes are **deleted**.  In the above example, if there was a campaign with a claim code of `test_2` already, it would be lost and this one would take its place.

## Step 2: Creating the Claim URL

There are two different ways to accomplish this step.  

### Method 1: Account Age-Based Verification

**Important:** This documentation is specifically for mail-service requests.  All public-facing traffic should first go through DMZ.

In the first method, we take a naive approach and operate just off of account IDs and reward IDs.  However, this leaves us open to an exploit.  We'll get to that in a bit, but the request to claim a reward with this system needs to be formulated as:

```
/mail/claim?accountId={id}&claimCode={the campaign.claimCode from Step 1}

HTTP 200
{
    "campaignMessage": {
        "accountId": "656f886c8be2aefc0e260fd0",
        "subject": "Hello, World!",
        "body": "This is a test of the email campaign rewards system",
        "attachments": [
            {
                "type": "currency",
                "rewardId": "hard_currency",
                "quantity": 100
            }
        ],
        "data": {},
        "expiration": 2648579400,
        "visibleFrom": 0,
        "icon": "",
        "banner": "",
        "status": 0,
        "internalNote": "testCampaign",
        "minimumAgeInSeconds": 86400,
        "claimCode": "test_2",
        "redirectUrl": "https://towersandtitans.com/",
        "id": "656f99cb740c40c66aebf2ec",
        "createdOn": 1701812684
    }
}
```

This approach is less secure than the Guid-based approach.  Because one of the parameters is the account ID, and links can be seen by players, it allows anyone to change the URL to any account ID they might know of (whether it's theirs or a friend's), and as a consequence rewards might be given out to people not yet subscribed.  The account age requirement prevents young accounts from abusing this, however.

We can harden our system and lock it down further by creating a unique ID for every reward, guaranteeing that every player's reward code is exclusive to just that player.

### Method 2: Guid-based promo codes

In this second method, we first create a linking between our claim code and a player ID.  Then we use the resulting GUID (globally unique identifier) in the URL instead.  

This functionality can be toggled in Dynamic Config using the key `useGuidCampaignFormat` and setting the value to `true` or `false`.

First, create the pairings for every claim code by sending all of the claimCodes we want to create for a specific account ID:

```
POST /admin/promoPairings
{
    "accountId": "deadbeefdeadbeefdeadbeef",
    "expiration": 1733385617,
    "claimCodes": [
        "test_1"
    ]
}

HTTP 200
{
    "guids": [
        {
            "accountId": "deadbeefdeadbeefdeadbeef",
            "promoCode": "test_1",
            "expiration": 1733385617,
            "id": "656e824f3250b731e361cde7",           // This is going to be our unique code
            "createdOn": 1701741135
        }
    ]
}
```

Now, a successful claim request should look like:

```
/mail/claim?claimCode={the campaign.claimCode from above}

HTTP 200
{
    "mailboxMessage": {
        "accountId": "deadbeefdeadbeefdeadbeef",
        "subject": "Hello, World!",
        "claimCode": "test_1",
        "body": "This is a test of the email campaign rewards system",
        "attachments": [
            {
                "type": "currency",
                "rewardId": "hard_currency",
                "quantity": 100
            }
        ],
        "data": {},
        "timestamp": 1701734417,
        "expiration": 1733385617,
        "visibleFrom": 1701734417,
        "icon": "",
        "banner": "",
        "status": 0,
        "internalNote": "testCampaign",
        "id": "656e7db5db6196f8d2a14eb0",
        "createdOn": 1701739957
    }
}
```

This approach requires passing the email management system much more data, since _every_ email has to have a _unique_ value.

This is a Platform-authoritative approach, however: Platform is dictating the GUIDs and assuming that they will be passed on to the email system.  Depending on the provider, this may not be possible or could be difficult to implement.

Some email providers may support a system where they are the authority on the GUID, where they create a unique ID and have a way to update our systems to reflect it.

As this secure approach is a rapid iteration to harden our campaign reward claims, it may need to change before it sees a release.

## Step 3: Creating the DMZ Link

This part is rather simple; instead of 

```
/mail/claim?claimCode=test_2&accountId=656f886c8be2aefc0e260fd0
```

use:

```
/dmz/rewards/claim?claimCode=test_2&accountId=656f886c8be2aefc0e260fd0
```

DMZ is a publicly viewable server, meaning that anyone looking for links in Marketplace or from their email will likely see it.  DMZ masks our end servers, but also has additional features - in this case, request forwarding and redirection handling.