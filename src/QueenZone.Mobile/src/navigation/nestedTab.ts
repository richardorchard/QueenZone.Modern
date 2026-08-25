/**
 * Cross-tab navigation helpers.
 *
 * React Navigation mounts an unvisited nested stack with the target screen as
 * its only route unless `initial: false` is set. Detail routes also hide the
 * tab bar, so that leaves iPhone users with no back control and no tabs.
 */

export type NestedTabParams<Screen extends string, Params = undefined> = Params extends undefined
  ? { screen: Screen; initial: false }
  : { screen: Screen; params: Params; initial: false };

export function nestedTabParams<Screen extends string>(
  screen: Screen,
): NestedTabParams<Screen>;
export function nestedTabParams<Screen extends string, Params extends object>(
  screen: Screen,
  params: Params,
): NestedTabParams<Screen, Params>;
export function nestedTabParams<Screen extends string, Params extends object>(
  screen: Screen,
  params?: Params,
): { screen: Screen; params?: Params; initial: false } {
  return params === undefined ? { screen, initial: false } : { screen, params, initial: false };
}

export function goBackOrFallback<Screen extends string>(
  navigation: {
    canGoBack: () => boolean;
    goBack: () => void;
    navigate: (name: Screen) => void;
  },
  fallbackScreen: Screen,
): void {
  if (navigation.canGoBack()) {
    navigation.goBack();
    return;
  }

  navigation.navigate(fallbackScreen);
}

/** Home-stack stories fall back to Home; News-stack stories fall back to News. */
export function storyLeaveFallback(routeNames: readonly string[] | undefined): 'Home' | 'NewsIndex' {
  return routeNames?.includes('Home') ? 'Home' : 'NewsIndex';
}

export function leaveStoryScreen(navigation: {
  canGoBack: () => boolean;
  goBack: () => void;
  navigate: (name: 'Home' | 'NewsIndex') => void;
  getState?: () => { routeNames?: readonly string[] } | undefined;
}): void {
  goBackOrFallback(navigation, storyLeaveFallback(navigation.getState?.()?.routeNames));
}
