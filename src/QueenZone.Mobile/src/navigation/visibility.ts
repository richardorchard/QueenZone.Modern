/**
 * Public vs member navigation contract.
 *
 * Matches the website's content boundary, not a mobile-only policy:
 * visitors can read archive, photography, forum threads, and fan-performance
 * listings; private messages and member write/settings surfaces require sign-in.
 */

export const publicTabNames = [
  'TodayTab',
  'NewsTab',
  'PhotosTab',
  'ForumTab',
  'YouTab',
] as const;

export const signedInOnlyTabNames = ['MessagesTab'] as const;

export type PublicTabName = (typeof publicTabNames)[number];
export type SignedInOnlyTabName = (typeof signedInOnlyTabNames)[number];
export type TabName = PublicTabName | SignedInOnlyTabName;

export const publicScreenNames = [
  'Today',
  'Biography',
  'BiographyChapter',
  'Discography',
  'Album',
  'Timeline',
  'FreddieTribute',
  'FanPerformances',
  'Story',
  'Search',
  'NewsIndex',
  'PhotoIndex',
  'PhotoViewer',
  'ForumIndex',
  'Thread',
  'Account',
  'Help',
  'SignIn',
] as const;

export const memberOnlyScreenNames = [
  'FanPerformanceDetail',
  'Inbox',
  'Conversation',
  'ComposeMessage',
  'Composer',
  'PhotoSubmit',
  'Profile',
  'Settings',
] as const;

export type MemberOnlyScreenName = (typeof memberOnlyScreenNames)[number];

export function getVisibleTabNames(isSignedIn: boolean): readonly TabName[] {
  if (!isSignedIn) {
    return publicTabNames;
  }

  return [
    'TodayTab',
    'NewsTab',
    'PhotosTab',
    'ForumTab',
    'MessagesTab',
    'YouTab',
  ];
}

export function isMemberOnlyScreen(name: string): boolean {
  return (memberOnlyScreenNames as readonly string[]).includes(name);
}

export const detailScreenNames = [
  'Story',
  'BiographyChapter',
  'Album',
  'Search',
  'PhotoViewer',
  'PhotoSubmit',
  'Thread',
  'Composer',
  'Conversation',
  'ComposeMessage',
  'FanPerformanceDetail',
  'Help',
  'Settings',
  'Profile',
  'SignIn',
] as const;

export function shouldHideTabBar(routeName: string | undefined): boolean {
  if (!routeName) {
    return false;
  }

  return (detailScreenNames as readonly string[]).includes(routeName);
}
