/** Gesture builders that support RNGH's `.runOnJS(...)`. */
export type PhotoZoomJsGesture<T> = {
  runOnJS: (enabled: boolean) => T;
};

/**
 * Keep pinch and double-tap on the JS thread.
 *
 * Reanimated 4 worklets abort on iOS for some gesture callbacks (same class as
 * gallery swipe, which already uses `.runOnJS(true)`). Writing shared values
 * from these JS handlers is allowed.
 */
export function runPhotoZoomOnJS<T extends PhotoZoomJsGesture<T>>(gesture: T): T {
  return gesture.runOnJS(true);
}
