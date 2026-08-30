import type { NavigatorScreenParams } from '@react-navigation/native';

export type SignInReturnTo = {
  tab: keyof RootTabParamList;
  screen: string;
  params?: object;
};

export type SignInParams = {
  returnTo?: SignInReturnTo;
};

export type HomeStackParamList = {
  Home: undefined;
  Quote: { id: number };
  Story: { id: number };
  Search: undefined;
  Profile: undefined;
  Settings: undefined;
  Contact: undefined;
  Inbox: undefined;
  Archived: undefined;
  Conversation: { id: string };
  ComposeMessage: undefined;
  SavedList: { kind: 'articles' | 'photographs' | 'offline' | 'history' };
  DeleteAccount: undefined;
  MySubmissions: undefined;
  SuggestNews: undefined;
};

export type NewsStackParamList = {
  NewsIndex: { refreshAt?: number } | undefined;
  Story: { id: number };
  Search: undefined;
};

export type PhotosStackParamList = {
  PhotoIndex: undefined;
  PhotoCategory: { slug: string; name?: string };
  PhotoViewer: { slug: string; picId: number; size?: string };
  PhotoSubmit: undefined;
  Search: undefined;
};

export type ArchiveStackParamList = {
  ArchiveHub: undefined;
  Articles: undefined;
  Biography: undefined;
  BiographyChapter: { id: number };
  Discography: undefined;
  Album: { id: number };
  Timeline: { focusId?: number } | undefined;
  FreddieTribute: undefined;
  FanPerformances: undefined;
  FanPerformanceDetail: { id: number };
  Story: { id: number };
  AboutArchive: undefined;
  Search: undefined;
};

export type ForumStackParamList = {
  ForumIndex: undefined;
  Category: { id: number; name?: string };
  Thread: { id: number | string; title?: string; postId?: number };
  Composer: {
    threadId?: number;
    threadTitle?: string;
    categoryId?: number;
    categoryName?: string;
    isLocked?: boolean;
  };
  Search: undefined;
};

export type RootTabParamList = {
  HomeTab: NavigatorScreenParams<HomeStackParamList>;
  NewsTab: NavigatorScreenParams<NewsStackParamList>;
  PhotosTab: NavigatorScreenParams<PhotosStackParamList>;
  ArchiveTab: NavigatorScreenParams<ArchiveStackParamList>;
  ForumTab: NavigatorScreenParams<ForumStackParamList>;
};

export type RootStackParamList = {
  Tabs: NavigatorScreenParams<RootTabParamList> | undefined;
  SignIn: SignInParams | undefined;
};

declare global {
  namespace ReactNavigation {
    interface RootParamList extends RootStackParamList {}
  }
}
