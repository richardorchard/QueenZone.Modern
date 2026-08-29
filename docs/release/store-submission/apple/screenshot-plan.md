# Screenshot production plan

## Required deliverables

Because the Expo configuration currently sets `ios.supportsTablet` to `true`, prepare both:

- iPhone 6.9-inch portrait: **1290 × 2796 px**, PNG or JPEG, no alpha.
- iPad 13-inch portrait: **2064 × 2752 px** (or Apple's accepted 2048 × 2732 alternative), PNG or JPEG, no alpha.

Produce six images for each device. Apple permits one to ten; six tells the story without repetition.

## Shot sequence and exact overlay copy

| Order | Real screen/state | Caption |
| --- | --- | --- |
| 1 | Home with hero and navigation visible | **Decades of Queen history** |
| 2 | Archive hub with its major collections | **Explore the complete archive** |
| 3 | News index or a strong news story | **News, restored and current** |
| 4 | Photography collection/grid | **Thousands of photographs** |
| 5 | Timeline, biography or discography | **Stories behind the music** |
| 6 | Forum index while signed out, or a safe seeded thread | **Join the QueenZone community** |

Optional seventh shot if the selected release is strong enough:

- On This Day widget installed on a clean Home Screen — **Queen history, every day**.

## Capture rules

- Capture the exact release-candidate build against production-safe data.
- Use a clean simulator/device with no personal notifications, names, email addresses or messages.
- Prefer public screens; never expose private-message content or real member information.
- Use the same appearance across the set (recommended: dark).
- Fix the status-bar time consistently and show full battery/signal.
- Do not show development banners, Metro, debug menus, localhost endpoints or test environment labels.
- Keep overlay captions outside critical UI and reproduce the text above verbatim.
- Preserve the app UI exactly. Marketing framing may add background and caption, but must not fabricate features.
- Export without alpha and verify pixel dimensions after composition.

## Capture dependency

Final current-build captures require the selected release candidate on an appropriate iPhone/iPad simulator runtime or physical TestFlight devices. Existing design-handoff screens are useful as visual references but must not be submitted as if they were current-build screenshots.
