# iOS TestFlight Push Verification

How to prove production APNs delivery on a real iPhone. This is receive +
record. TestFlight publish is already done; do not run
`.github/workflows/publish-ios-testflight.yml` for this check.

Local toolchain setup (Node, Xcode, simulators) lives in
[`mobile-development-environment.md`](mobile-development-environment.md). That
guide is not required to receive a TestFlight push.

## Production APNs only

The TestFlight IPA is a distribution build. Apple issues it a **production**
device token. The live API must send to production APNs.

| What | Value |
| --- | --- |
| TestFlight workflow | `IOS_APS_ENVIRONMENT=production` in [`.github/workflows/publish-ios-testflight.yml`](../.github/workflows/publish-ios-testflight.yml) (already set; do not change it) |
| App API origin | `https://www.queenzone.org` (`EXPO_PUBLIC_APP_ENV=staging` still uses that public origin) |
| App Service | `PushNotifications__Apns__Environment=production` (see [`bitwarden-secrets.md`](bitwarden-secrets.md)) |
| Apple endpoint | `https://api.push.apple.com` |

Do not point TestFlight at sandbox APNs or `aps-environment: development`. A
sandbox token on a production IPA (or the reverse) fails with `BadDeviceToken`.
Sandbox is only for local development-signed builds.

Expo prebuild still stamps `aps-environment=development`; the App Store archive
replaces it with `production`. The publish workflow already verifies the
exported IPA. That is not a reason to send TestFlight traffic to sandbox.

## What you need

- A real iPhone. Push does not work in the Simulator.
- The current QueenZone internal TestFlight build installed from Apple's
  TestFlight app.
- Two member accounts. The receiver is the TestFlight sign-in. The sender is
  someone else — you never get a push for your own message or your own reply.
- For news only: an admin session on `https://www.queenzone.org/admin/news`.

Trigger every category against the same live API the TestFlight app already
uses (`https://www.queenzone.org`). A local or in-memory host will not reach
that device token.

## Smallest first check (private message)

Private messages are default-on and fan out to one recipient. Use this before
forum or news.

1. On the iPhone, open the TestFlight build and sign in as the **receiver**.
2. Allow notifications when the OS prompt appears. Sign-in is what registers
   the APNs token with `https://www.queenzone.org`. If you previously denied
   the prompt, enable QueenZone in iOS Settings → Notifications, then
   foreground the app so it can register.
3. Background the app (Home or lock). Foreground still shows an in-app banner,
   but the first proof should be a system notification.
4. On a second account (website or another device), open
   `https://www.queenzone.org/messages/compose` (or Messages → **New message**)
   and send a DM to the receiver. A reply in an existing thread also fires.
5. Confirm the lock-screen / notification-center alert: title **New private
   message**, body **You have a new message.**
6. Tap it. The app should open that conversation (`#851`).

A compose or reply from the receiver's own account sends nothing to that
account.

## Categories

Dispatch is inline on the live write path
([ADR 0014](decisions/0014-push-notification-transport-and-dispatch.md)).
Empty audience or no stored device token is a silent skip (no log). Missing
APNs credentials on App Service log a Warning
(`PushNotifications APNs credentials are not configured; skipping APNs sends
for category {Category}.`) and skip. Successful sends are not logged. A
provider error logs a Warning with member id and category (token redacted) and
drops the send.

### Private message (default on)

- **Fires when:** another member DMs you (new conversation or reply).
- **Does not fire:** you message yourself; you are the sender.
- **Preference:** default on. Mute only from the TestFlight app: Profile
  (Home masthead avatar) → **Account settings** → Notifications → **Private
  messages**.
- **Trigger on live:** `https://www.queenzone.org/messages/compose`, or
  Messages → **New message** / reply in a thread, signed in as someone else.
- **Tap-through:** Conversation screen.

### Forum reply (default on, Watch required)

- **Fires when:** someone else replies on a topic you **Watched**.
- **Does not fire:** you start a topic; you reply (author is excluded even if
  you Watch); you never Watch.
- **Preference:** default on, but Watch is a separate opt-in. The mobile
  Settings toggle **Forum replies** does not subscribe you to a topic.
- **Trigger on live:**
  1. Signed in as the receiver, open the topic in the TestFlight app or at
     `https://www.queenzone.org/forum/topic/{id}/{slug}` and tap **Watch topic**.
  2. Signed in as someone else, post a reply on that topic (website or app).
- **Tap-through:** forum thread (`topicId`, optional `postId`).

### News (default off, first publish only)

- **Fires when:** an unpublished article is published, and only to members who
  have opted in. News uses stored enabled rows only; a member who never
  touched the toggle is not in the audience.
- **Does not fire:** create/save draft; edit a published article; Publish on
  an already-published row. Unpublish then Publish is another
  unpublished→published transition and will send again — use a throwaway draft
  for this check.
- **Preference:** default off. The toggle is mobile Settings only: Profile →
  **Account settings** → Notifications → **News**. Turn it on on the
  TestFlight device before publishing.
- **Trigger on live:** as admin, `https://www.queenzone.org/admin/news` →
  **Create article** → Save (draft) → **Publish** on the list or edit page.
- **Tap-through:** news story (`articleId`). Title is the article title; body
  is **New article published.**

## Tap-through (`#851`)

For each category that arrives, tap the system notification (app backgrounded
or cold-started) and confirm the matching screen. A foreground arrival shows
an in-app banner; tapping that banner should open the same destination.

| Category | Expected screen |
| --- | --- |
| Private message | Conversation |
| Forum reply | Thread |
| News | Story |

## If nothing arrives

1. Confirm a real device, TestFlight install, signed-in receiver, and
   notifications allowed.
2. Confirm the sender is a different member and the category rules above.
3. Empty audience (nobody Watching / news not opted in) or no stored token:
   silent skip. There is no success log to look for.
4. App Service Warning `PushNotifications APNs credentials are not
   configured` means TeamId / KeyId / PrivateKeyPem are missing. Do not
   "fix" that by switching Environment to sandbox.
5. App Service Warning `APNs send failed ... BadDeviceToken` usually means
   the token environment does not match production. Leave TestFlight and
   `PushNotifications__Apns__Environment` on production.

## Device-receive record

Richard runs this on a real iPhone. Do not treat this table as done until he
fills it in.

| Category | Build (TestFlight / CFBundleVersion) | Delivered | Tap-through | Date | Notes |
| --- | --- | --- | --- | --- | --- |
| Private message | | | | | |
| Forum reply | | | | | |
| News | | | | | |

## Related

- [ADR 0014](decisions/0014-push-notification-transport-and-dispatch.md) —
  direct APNs/FCM, inline dispatch
- [`bitwarden-secrets.md`](bitwarden-secrets.md) — APNs App Service setting
  names (production)
- [`src/QueenZone.Mobile/README.md`](../src/QueenZone.Mobile/README.md) —
  how TestFlight builds are published (not this ticket)
