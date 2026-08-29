# QueenZone mobile store submission packs

Version-neutral preparation material for the first public QueenZone mobile release. Nothing in this folder submits a build or sends a listing for review.

## Packs

| Store | Folder | Current state |
| --- | --- | --- |
| Apple App Store | [`apple/`](apple/) | Metadata, privacy/review drafts and opaque 1024px icon complete; final iPhone/iPad screenshots and private reviewer details pending |
| Google Play | [`google-play/`](google-play/) | Listing, Data safety/app-content drafts, Play icon and feature graphic complete; final phone screenshots and private reviewer details pending |

The packs deliberately do not contain passwords, API keys, signing material, service-account JSON or personal reviewer contact details. Enter those directly into the store consoles.

Store icons and the Google Play feature graphic are deterministic exports. Regenerate them after `npm ci` with `./docs/release/store-submission/generate-assets.sh` (ImageMagick required).

## Shared product facts

- App name: **QueenZone**
- iOS bundle ID / Android application ID: `org.queenzone.mobile`
- Production website and API: `https://www.queenzone.org`
- Privacy policy: `https://www.queenzone.org/privacy`
- Data-deletion instructions: `https://www.queenzone.org/data-deletion`
- Support/contact: `https://www.queenzone.org/contact`
- Current development version: `0.1.0`
- Recommended first public store version: `1.0`
- Price: free
- QueenZone is an independent fan archive and community and is not affiliated with Queen or its representatives.

## Working rule

Store metadata may be loaded as a draft before release scope is final. Final screenshots, privacy declarations, content ratings and reviewer instructions must be reconciled against the exact build selected for submission. Screenshots must show real release-candidate UI and safe data; the older design-handoff mockups are not submission assets.

## Authoritative platform references

- [Apple: add an app record](https://developer.apple.com/help/app-store-connect/create-an-app-record/add-a-new-app)
- [Apple: screenshot specifications](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications/)
- [Apple: manage app privacy](https://developer.apple.com/help/app-store-connect/manage-app-information/manage-app-privacy/)
- [Google Play: create and set up an app](https://support.google.com/googleplay/android-developer/answer/9859152)
- [Google Play: preview asset requirements](https://support.google.com/googleplay/android-developer/answer/9866151)
- [Google Play: prepare an app for review](https://support.google.com/googleplay/android-developer/answer/9859455)
- [Google Play: Data safety](https://support.google.com/googleplay/android-developer/answer/10787469)

## Before either public submission

1. Choose the release-candidate feature set and public version.
2. Run the normal repository and signed-build verification.
3. Exercise authentication, account deletion, moderation/reporting, photo submission, notifications, widgets and sharing on physical devices.
4. Capture fresh screenshots from that build.
5. Re-audit included SDKs and runtime configuration against both privacy forms.
6. Confirm content rights for every archive, photo and audio surface shown or distributed.
7. Test dedicated reviewer credentials on a clean installation.
8. Save all console changes as drafts and have the product owner review them before the final review action.
