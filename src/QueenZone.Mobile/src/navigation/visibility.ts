/**
 * Public vs member navigation contract.
 *
 * Tab IA follows the v2 mobile handoff: five tabs on both platforms
 * (Home · News · Photography · Archive · Forum). Profile, settings, and
 * private messages sit behind the Home masthead avatar — never a sixth tab.
 *
 * Member-only *screens* still match the website's content boundary.
 */

export const publicTabNames = [
  'HomeTab',
  'NewsTab',
  'PhotosTab',
  'ArchiveTab',
  'ForumTab',
] as const;

export const signedInOnlyTabNames = [] as const;

export type PublicTabName = (typeof publicTabNames)[number];
export type SignedInOnlyTabName = (typeof signedInOnlyTabNames)[number];
export type TabName = PublicTabName | SignedInOnlyTabName;

export const publicScreenNames = [
  'Home',
  'ArchiveHub',
  'Stories',
  'AboutArchive',
  'Biography',
  'BiographyChapter',
  'Discography',
  'Album',
  'Timeline',
  'FreddieTribute',
  'FanPerformances',
  'FanPerformanceDetail',
  'Story',
  'Search',
  'NewsIndex',
  'PhotoIndex',
  'PhotoCategory',
  'PhotoViewer',
  'ForumIndex',
  'Category',
  'Thread',
  'Profile',
  'Contact',
  'SignIn',
] as const;

export const memberOnlyScreenNames = [
  'Inbox',
  'Conversation',
  'ComposeMessage',
  'Composer',
  'PhotoSubmit',
  'Settings',
  'SavedList',
  'DeleteAccount',
] as const;

export type MemberOnlyScreenName = (typeof memberOnlyScreenNames)[number];

export function getVisibleTabNames(_isSignedIn: boolean): readonly TabName[] {
  return publicTabNames;
}

export function isMemberOnlyScreen(name: string): boolean {
  return (memberOnlyScreenNames as readonly string[]).includes(name);
}

export const detailScreenNames = [
  'Story',
  'Stories',
  'Biography',
  'BiographyChapter',
  'Discography',
  'Album',
  'Timeline',
  'FreddieTribute',
  'FanPerformances',
  'AboutArchive',
  'Search',
  'PhotoViewer',
  'PhotoSubmit',
  'Thread',
  'Composer',
  'Conversation',
  'ComposeMessage',
  'FanPerformanceDetail',
  'Contact',
  'Settings',
  'Profile',
  'SignIn',
  'Inbox',
  'SavedList',
  'DeleteAccount',
] as const;

export function shouldHideTabBar(routeName: string | undefined): boolean {
  if (!routeName) {
    return false;
  }

  return (detailScreenNames as readonly string[]).includes(routeName);
}
