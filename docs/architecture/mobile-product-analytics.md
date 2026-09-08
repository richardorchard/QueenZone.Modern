# Mobile product analytics

QueenZone uses TelemetryDeck for a narrow, anonymous view of mobile adoption.
Sentry remains responsible for crashes and diagnostics. Store consoles remain
the source for downloads and installs.

## Questions and event catalogue

| Product question | Event | Metadata | Maximum frequency |
| --- | --- | --- | --- |
| How many app installations are active? | `app.active` | App version/build, platform, locale and device region | Once per installation per UTC day |
| Which main sections are used most? | `section.viewed` | The allowlisted `section`, plus the common metadata above | Once per change of top-level section |

Allowed `section` values are `home`, `news`, `photography`, `archive`, and
`forum`. Detail screens remain within their parent section. Repeated navigation
inside one section does not emit another event.

The design targets fewer than 20 events per monthly active installation on
average. At that rate, the 50,000-event free allowance supports about 2,500
monthly active installations. When the allowance is exhausted, analytics can
be incomplete; no user-facing feature depends on delivery.

## Privacy boundary

- `clientUser` is a random, analytics-only installation UUID. The SDK hashes it
  before transmission. It is not a QueenZone member or device-push identifier.
- No account identifiers, content identifiers, URLs, route parameters, search
  text, message bodies, filenames, or free-form user content are sent.
- Region is the device's coarse two-letter preference. TelemetryDeck may also
  derive country-level origin during ingestion. QueenZone requests no location
  permission and collects no precise location.
- TelemetryDeck failures are swallowed locally and are not sent to Sentry.
- No client or signal is created until the user selects **Allow anonymous
  analytics**. Refusal is stored and does not trigger repeat prompts.
- The public Profile screen exposes **Analytics preferences** to signed-in and
  signed-out users. Withdrawal stops future signals and deletes the local
  analytics installation ID and daily-active marker.
- Normal development, tests, and smoke builds do not send events because the
  App ID is unset. Configured non-production builds use TelemetryDeck test mode.

## Configuration and ownership

`EXPO_PUBLIC_TELEMETRYDECK_APP_ID` is a public identifier baked into Expo
`extra`. Store workflows read it from the `TELEMETRYDECK_APP_ID` variable in
the publishing GitHub Environments. The app keeps analytics disabled when it
is absent.

The TelemetryDeck account must remain owned by Richard with no payment method
or paid subscription. The free plan is dashboard-only for this feature; API
reporting and automated exports are out of scope.

Previously delivered events are anonymous and cannot be selected for
per-person deletion. TelemetryDeck currently publishes no guaranteed
cold-storage deletion schedule and says it expects to delete events after
7–10 years. This retention position must be rechecked before each store
submission.
