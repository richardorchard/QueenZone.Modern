# Google Play Data safety draft

This is a conservative source-level draft, not a legal conclusion. Reconcile it with the exact release AAB, production backend and current Google Play form immediately before review.

## Collection and sharing overview

- **Does the app collect or share required user-data types?** Yes.
- **Is all transmitted user data encrypted in transit?** Yes, for production HTTPS/API and provider traffic; verify the final network-security configuration.
- **Can users request deletion?** Yes, in-app and through `https://www.queenzone.org/data-deletion`.
- **Tracking / advertising use:** None identified in the native app. The website's analytics/advertising disclosures must not be copied to the native form unless the native build actually includes them.
- **Sale of data:** No.

## Likely declarations

| Play data category | Collected | Shared | Required or optional | Purpose |
| --- | --- | --- | --- | --- |
| Name | Yes | With authentication provider only as needed | Required for account/profile; optional for public contact | Account management, app functionality, support |
| Email address | Yes | With chosen authentication provider as needed | Required for account; optional for contact form | Account management, authentication, support |
| User IDs | Yes | With service providers operating the app | Required when signed in | Account management, app functionality, security |
| Photos | Yes when submitted | With hosting/storage providers | Optional | Avatar, forum and moderated photo submission |
| Other user-generated content | Yes | With hosting/service providers; forum content becomes public by user action | Optional | Forum posts, messages, suggestions, contact requests |
| App interactions | Possibly | Sentry when enabled | Automatic if production tracing is enabled | Analytics and app functionality |
| Crash logs | Possibly | Sentry when enabled | Automatic if production reporting is enabled | Diagnostics |
| Diagnostics / performance | Possibly | Sentry when enabled | Automatic if production tracing is enabled | Diagnostics and analytics |
| Device or other IDs | Yes | FCM and hosting/service providers | Automatic for opted-in push | Notifications, security, app functionality |

“Shared” in this table is deliberately conservative. In the Play form, apply Google's service-provider and user-initiated-transfer definitions carefully; a transfer may qualify for an exception and therefore not be declared as sharing even though it remains described in the privacy policy.

## Security and handling checks

- Authentication tokens are stored with secure platform storage.
- No password is received from external identity providers.
- Push tokens are associated with a signed-in member/device and can be revoked.
- User-selected photos leave the device only after an explicit submission action.
- Private-message bodies, authentication tokens, email addresses and uploaded image bytes must not be recorded in Sentry breadcrumbs or telemetry.
- Confirm whether Sentry captures route names, IP addresses, device identifiers or user identity in the final configuration.
- Confirm account deletion covers mobile sessions, push subscriptions and uploaded avatar references as documented.

## Form decisions requiring product-owner confirmation

- Whether optional contact-form name/email are represented separately from account data.
- Whether public forum posts/photo submissions qualify as user-initiated sharing exceptions.
- Whether Sentry is enabled in the selected build and exactly which data types it receives.
- Whether the Data safety form should claim an independent security review; do not claim one without a current qualifying assessment.

