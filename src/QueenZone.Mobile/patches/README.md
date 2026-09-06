# Dependency patches

`react-native-android-widget+0.22.1.patch` restores direct `RemoteViews` bitmap delivery for the
top-level widget image. Version 0.22.1 writes that image to private app storage and gives the
launcher a content URI. On affected launchers, the independently delivered tap overlay remains
active while the URI-backed image is blank.

The patch does not change list widgets or QueenZone's widget content and rotation logic. Remove it
after an upstream release provides a device-verified fix for top-level image delivery.
