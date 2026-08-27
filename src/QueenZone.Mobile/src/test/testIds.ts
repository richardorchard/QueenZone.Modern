/** Stable Maestro / RTL selectors. Prefer these over visible copy. */
export const testIds = {
  tabHome: 'tab-home',
  tabNews: 'tab-news',
  tabPhotos: 'tab-photos',
  tabArchive: 'tab-archive',
  tabForum: 'tab-forum',

  homeScreen: 'home-screen',
  homeHero: 'home-hero',
  homeSearch: 'home-search',
  homeProfile: 'home-profile',

  newsScreen: 'news-screen',
  newsStoryScreen: 'news-story-screen',
  newsStoryBack: 'news-story-back',
  newsYearRail: 'news-year-rail',
  newsSuggest: 'news-suggest',

  suggestNewsScreen: 'suggest-news-screen',
  suggestNewsUrl: 'suggest-news-url',
  suggestNewsTitle: 'suggest-news-title',
  suggestNewsNotes: 'suggest-news-notes',
  suggestNewsSubmit: 'suggest-news-submit',
  suggestNewsCancel: 'suggest-news-cancel',
  suggestNewsSignIn: 'suggest-news-sign-in',
  suggestNewsRetry: 'suggest-news-retry',
  suggestNewsChooser: 'suggest-news-chooser',
  suggestNewsSuccess: 'suggest-news-success',

  searchTypeFilters: 'search-type-filters',

  photosScreen: 'photos-screen',
  photoCategoryScreen: 'photo-category-screen',
  photoViewerScreen: 'photo-viewer-screen',

  archiveHubScreen: 'archive-hub-screen',
  searchScreen: 'search-screen',
  searchInput: 'search-input',

  forumScreen: 'forum-screen',
  forumCategoryScreen: 'forum-category-screen',
  forumThreadScreen: 'forum-thread-screen',
  forumThreadWatch: 'forum-thread-watch',
  forumNewThread: 'forum-new-thread',

  profileSignedOut: 'profile-signed-out',
  profileSignedIn: 'profile-signed-in',
  profileDisplayName: 'profile-display-name',
  profileBack: 'profile-back',
  profileRestoring: 'profile-restoring',
  fanPerformanceSessionRestoring: 'fan-performance-session-restoring',

  signInClose: 'sign-in-close',

  memberGate: 'member-gate',
  inboxScreen: 'inbox-screen',
  archivedScreen: 'archived-screen',

  notificationBanner: 'notification-banner',

  settingsNotifyForumReply: 'settings-notify-forum-reply',
  settingsNotifyPrivateMessage: 'settings-notify-private-message',
  settingsNotifyNews: 'settings-notify-news',
} as const;

export type TestId = (typeof testIds)[keyof typeof testIds];
