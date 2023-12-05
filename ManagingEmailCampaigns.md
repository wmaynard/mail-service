# Managing Email Campaigns

Mailbox now supports the ability to grant players rewards based on engagement with marketing emails.  The goal is to send our subscribers messages that allow them to get rewards, driving engagement in the game.  There are two approaches to this; one is insecure but fast to roll out; the other is much more secure but requires more work to integrate with marketing email systems.  Both approaches require some basic steps.

## Step 1: Defining Message Rewards

Before we can send rewards out to players, we have to define what the messages will look like when delivered to players.  We do that by defining **campaigns.**  To do this, we need a new field, `claimCode`, which is used as a unique identifier for the message:


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
            "timestamp": 1701734417,
            "expiration": 1733385617,
            "visibleFrom": 1701734417,
            "icon": "",
            "banner": "",
            "internalNote": "testCampaign",
            "claimCode": "test_1"                         // <---
        }
    ]
}

HTTP 200
{
    "campaigns": [
        {
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
            "id": "656e7822b83dedcdeabbe225",
            "createdOn": 1701738531
        }
    ]
}
```

## Step 2: Creating the Claim URL

There are two different ways to accomplish this step.  

### Method 1: Naive

**Important:** This documentation is specifically for mail-service requests.  All public-facing traffic should first go through DMZ.

In the first method, we take a naive approach and operate just off of account IDs and reward IDs.  However, this leaves us open to an exploit.  We'll get to that in a bit, but the request to claim a reward with this system needs to be formulated as:

```
/mail/claim?accountId={id}&claimCode={the campaign.claimCode from Step 1}

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

**Why is this insecure?**

We plan on doling out increasingly-better rewards as time moves on.  Since the URL is inspectable by anyone who receives it, it will only take minimal effort for an enterprising player to realize that they can make a script to grant rewards to hundreds or thousands of players.  This is especially problematic when later rewards are much more valuable, and we may end up in a situation where brand new players (or duplicate accounts!) are able to spam freebies.  As an example:

* I start an account, I make it to 30d retention and various rewards for doing so.
* I sign up for 10 alt accounts.  The minute I get my first email, I know what my new account IDs are.
* I copy/paste all the links from my first account in my browser, but with my new account IDs.
* I have 10 copies of every reward we've defined on day 1 for these accounts.

We can harden our system and lock it down by creating a unique ID for every reward.

### Method 2: Secure

In this second method, we first create a linking between our claim code and a player ID.  Then we use the resulting GUID (globally unique identifier) in the URL instead.

This functionality can be toggled in Dynamic Config using the key `useSecureCampaignFormat` and setting the value to `true` or `false`.

First, create the pairings for every claim code:

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