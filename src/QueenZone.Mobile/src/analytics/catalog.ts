export const analyticsSections = [
  'home',
  'news',
  'photography',
  'archive',
  'forum',
] as const;

export type AnalyticsSection = (typeof analyticsSections)[number];

type NavigationStateLike = {
  index?: number;
  routes?: {
    name?: string;
    state?: NavigationStateLike;
  }[];
};

const sectionByTab: Readonly<Record<string, AnalyticsSection>> = {
  HomeTab: 'home',
  NewsTab: 'news',
  PhotosTab: 'photography',
  ArchiveTab: 'archive',
  ForumTab: 'forum',
};

/**
 * Resolve only the five public product sections. Detail route names and route
 * parameters never leave the app.
 */
export function sectionFromNavigationState(
  state: NavigationStateLike | undefined,
): AnalyticsSection | null {
  let current = state;

  while (current?.routes?.length) {
    const index = Math.min(
      Math.max(current.index ?? current.routes.length - 1, 0),
      current.routes.length - 1,
    );
    const route = current.routes[index];
    if (!route) {
      return null;
    }

    if (route.name && sectionByTab[route.name]) {
      return sectionByTab[route.name];
    }
    current = route.state;
  }

  return null;
}

/** Device preference only; this is not GPS or precise location. */
export function regionFromLocale(locale: string): string | null {
  try {
    const region = new Intl.Locale(locale).region;
    return region && /^[A-Z]{2}$/.test(region) ? region : null;
  } catch {
    const match = locale.match(/[-_]([A-Za-z]{2})(?:[-_]|$)/);
    return match?.[1]?.toUpperCase() ?? null;
  }
}
