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

  photosScreen: 'photos-screen',
  photoCategoryScreen: 'photo-category-screen',
  photoViewerScreen: 'photo-viewer-screen',

  archiveHubScreen: 'archive-hub-screen',
  searchScreen: 'search-screen',
  searchInput: 'search-input',

  forumScreen: 'forum-screen',
  forumCategoryScreen: 'forum-category-screen',
  forumThreadScreen: 'forum-thread-screen',
  forumNewThread: 'forum-new-thread',

  profileSignedOut: 'profile-signed-out',
  profileSignedIn: 'profile-signed-in',
  profileDisplayName: 'profile-display-name',
  profileBack: 'profile-back',

  signInClose: 'sign-in-close',

  memberGate: 'member-gate',
  inboxScreen: 'inbox-screen',
} as const;

export type TestId = (typeof testIds)[keyof typeof testIds];
