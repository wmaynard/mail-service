# Mailbox Service
A service for in-game mailbox functionality.

# Introduction
This service allows for managing player `inboxes`, `messages`, and `global messages`. These `messages` can be `fetched`,
`claimed`, or `expired` from a player perspective. An admin may perform more actions, including `sending` `messages`. In
addition, `global messages` may be `sent`, `edited`, or manually `expired` by an admin. If a message is `edited` or `expired` in 
this fashion, a record of previous versions of this `message` is stored for logging purposes.

A `message` is directed specifically to a user or group of users. This can include a `subject`, `body`, `attachments`, an
`expiration time`, and an `image` if applicable. `Global messages` have the additional optional `eligibility` property.

`Inboxes` are attached to a player account by `account ID` and contain all relevant `messages` for that player. This should
be instantiated at the same time that the player account is created for the purposes of `global message` `eligibility`.
There are currently no limitations for how many `messages` may be stored, but `expired messages` will be routinely
cleaned up from the database after a specific amount of time determined by an environment variable.

# Required Environment Variables
| Variable | Description |
| ---: | :--- |
| GRAPHITE | Link to hosted _graphite_ for analytics and monitoring. |
| LOGGLY_URL | Link to _Loggly_ to analyze logs in greater detail. |
| MONGODB_NAME | The _MongoDB_ name which the service connects to. |
| MONGODB_URI | The connection string for the environment's MongoDB. |
| RUMBLE_COMPONENT | The name of the service. |
| RUMBLE_DEPLOYMENT | Signifies the deployment environment. |
| RUMBLE_KEY | Key to validate for each deployment environment. |
| RUMBLE_TOKEN_VALIDATION | Link to current validation for player tokens. |
| RUMBLE_TOKEN_VERIFICATION | Link to current validation for admin tokens. Will include player tokens in the future. |
| VERBOSE_LOGGING | Logs in greater detail. |

# Configurable Variables
| Variable | Description |
| ---: | :--- |
| INBOX_CHECK_FREQUENCY_SECONDS | Frequency at which the service scans `inboxes` for expired `messages`. |
| INBOX_DELETE_OLD_SECONDS | Amount of time expired `messages` are held in `inboxes` before deletion. |

# Glossary
| Term | Description |
| ---: | :--- |
| Account ID (aid) | An `aid` is uniquely generated by MongoDB and refers to a specific player.|
| Token | Used for `Authorization` for requests. This is fetched from `player-service` and should be included as a `Bearer {token string}`. Certain endpoints require admin tokens. |
| Inbox | Contains relevant `messages` attached to a specific account ID. A `timestamp` is included denoting creation time. |
| Message | A normal `message` sent to specific accounts. It contains information related to the `message` itself as well as any `attachments` to be claimed. |
| Global Message | A special type of `message` that can be sent to all eligible players. Admins can perform additional actions to existing `global messages`. |
| Subject | A header typically to denote the main reason of a `message`. |
| Body | Contains details related to the `message`. |
| Attachments | Contains a list of any `attachments` that may be sent along with the `message`. |
| Attachment | Formatted by `Type` `RewardId` `Quantity` |
| Timestamp | Used to record the creation time of relevant objects, formatted as a `Unix timestamp`. |
| Expiration | Used to denote when a `message` will be `expired`, and thus not visible or `claimable`. Formatted as a `Unix timestamp`. |
| VisibleFrom | A `Unix timestamp` that makes `messages` visible. This allows `messages` to be sent before they are intended to be received. |
| Image | A link to an `image` that may be displayed with the `message`. |
| Status | Signifies if a `message` is either `UNCLAIMED` or `CLAIMED`. |
| Previous Versions | In the event that a `global message` is `edited` or manually `expired`, previous versions of the `message` are recorded for logging purposes. |
| ForAccountsBefore | Specifically for `global messages`, this gives admins the ability to only send `global messages` to `inboxes` created before a specific time formatted in `Unix Time`. This can be `null` to allow eligibility for all. |
| Claim | Action that allows a user to `claim` any `UNCLAIMED` `messages` in their `inbox`. |
| Send | Action that allows an admin to send new `messages` or `global messages` to users. |
| Edit | Action that allows an admin to modify all fields of existing `global messages`. |
| Expire | Action that allows an admin to effectively _delete_ a `global message`. It stays in MongoDB for logging purposes. |

# Using the Service
All non-health endpoints require a valid token from `player-service`. The admin endpoints require a valid admin token.
Requests to these endpoints should have an `Authorization` header with a `Bearer {token}`, where token is the aforementioned `player-service` token.
Tokens also supply player `account IDs` where applicable.

All `timestamps` in the service are in the format of a `Unix timestamp`. This is to allow consistency and reduce confusion between time zones.

# Endpoints
All endpoints are reached with the base route `/mail/`. Any following endpoints listed are appended on to the base route.

**Example**: `GET /mail/health`

## Top Level
No tokens are required for this endpoint.

| Method | Endpoint | Description | Required Parameters | Optional Parameters |
| ---: | :--- | :--- | :--- | :--- |
| GET | `/health` | Health check on the status of the following services: `InboxService`, `GlobalMessageService` |  |  |

## Inbox
All non-health endpoints require a valid token.

| Method | Endpoint | Description | Required Parameters | Optional Parameters |
| ---: | :--- | :--- | :--- | :--- |
| GET | `/inbox/health` | Health check on the status of the following service: `InboxService` |  |  |
| GET | `/inbox` | Using the `accountID` from the required token, fetches the `messages` in the player's `inbox`. If one does not exist, creates the `inbox`. The `inbox` is updated before being returned. |  |  |
| PATCH | `/inbox/claim` | Update the corresponding message to `CLAIMED` `status`. If `messageId` is not included or is `null`, all `messages` are updated to `CLAIMED`. |  | *string*`messageId` |

### Notes
A `messageId` is uniquely generated by MongoDB and attempting to claim one that does not exist in the inbox attached to the user will not perform any actions.

## Admin
All non-health endpoints require a valid admin token.

| Method | Endpoint | Description | Required Parameters | Optional Parameters |
| ---: | :--- | :--- | :--- | :--- |
| GET | `/admin/health` | Health check on the status of the following services: `InboxService`, `GlobalMessageService` |  |  |
| GET | `/admin/global/messages` | Fetches all active `global messages`. |  |  |
| POST | `/messages/send` | Sends a `message` to a list of `accountIds`. | *List*<*string*>`accountIds`<br />*Message*`message` |  |
| POST | `/global/messages/send` | Sends a `global message` to be fetched by all eligible users. | *GlobalMessage*`globalMessage` |  |
| PATCH | `/global/messages/edit` | Updates any applicable fields for a `global message`. | *string*`messageId` | *string*`subject`<br />*string*`body`<br />*List*<*Attachment*>`attachments`<br />*long*`expiration`<br />*long*`visibleFrom`<br />*string*`image`<br />*StatusType*`status`<br />*long*`forAccountsBefore` |
| PATCH | `/global/messages/expire` | _Deletes_ a `global message` by manually expiring it. The `global message` will remain in MongoDB until cleaned up. | *string*`messageId` |  |

### Notes
An `attachment` has two required properties: an *string* `Type` and a *string* `RewardId`. It also has an optional property *int* `Quantity`. This defaults to 1 if left out.

**`Attachment` Example**:
```
{
    "Type": "currency",
    "RewardId": soft_currency,
    "Quantity": 1500
}
```

Expired `global messages` are currently not fetched. In the future, perhaps there may be an optional parameter in the request body allowing for such `global messages` to be fetched.
To supply a `message` or a `global message` to be sent out, all fields must be present and filled in, with the exception of `attachments`.

**`Message` Example**:
```
{
    "accountIds": [
        "6140bd998caf79f468e6f8a6"
    ],
    "message": {
        "subject": "Test",
        "body": "Test body",
        "attachments": [],
        "expiration": 1635663600,
        "visibleFrom": 1635404400,
        "image": "testimage"
    }
}
```
A `message` has a `timestamp` property automatically set to the current time as a `Unix timestamp`. It also has a `status` property automatically set to `UNCLAIMED`.

**`GlobalMessage` Example**:
```
{
    "globalMessage": {
        "subject": "Test",
        "body": "Test body",
        "attachments": [
            {"Type": "currency", "RewardId": soft_currency, "Quantity": 1500}
            {"Type": "hero", "RewardId": warlord, "Quantity": 2}
        ],
        "expiration": 1635563500,
        "visibleFrom": 1635550000,
        "image": "testimage",
        "forAccountsBefore": null
    }
}
```
A `global message` also has the same automatically set properties `timestamp` and `status` set in the same was as for a `message`.
In addition, it has a `forAccountsBefore` property that may either be a `Unix timestamp` or `null`. A `Unix timestamp` will compare this `global message`
creation time to an `inbox` creation time to determine if the user connected to that `inbox` is eligible for the global message. A `null` value
simply allows all `inboxes` to receive the `global message`.

When editing a `global message`, admins have the ability to modify any or all of the relevant parameters. In the event that the admin wishes to modify
the `status` field, the accepted values are `UNCLAIMED` or `CLAIMED`. Any invalid types or `null` passed for any of the parameters will result in no change.
Changes to the `global message` are reflected upon all copies in all `inbox` collections.

# Future Updates
- The current `Attachment` model is basic. Any required specifications for this model can be filled out when needed.
- Claiming a message does not do anything to a player wallet. This needs to be implemented based on the response values.
- If admins wish to be able to fetch expired `global messages`, it is possible to implement an optional parameter to do so.

# Troubleshooting
- Any issues should be recorded as a log in _Loggly_. Please reach out if something does not work property to figure out the issue.