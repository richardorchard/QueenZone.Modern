import type { NavigatorScreenParams } from '@react-navigation/native';

export type HomeStackParamList = {
  Home: undefined;
  Search: undefined;
  Profile: undefined;
  Settings: undefined;
  SignIn: undefined;
  Help: undefined;
  Inbox: undefined;
  Conversation: { id: string };
  ComposeMessage: undefined;
  SavedList: { kind: 'articles' | 'photographs' | 'offline' | 'history' };
};

export type NewsStackParamList = {
  NewsIndex: undefined;
  Story: { id: number };
  Search: undefined;
};

export type PhotosStackParamList = {
  PhotoIndex: undefined;
  PhotoViewer: { id: string };
  PhotoSubmit: undefined;
  Search: undefined;
};

export type ArchiveStackParamList = {
  ArchiveHub: undefined;
  Stories: undefined;
  Biography: undefined;
  BiographyChapter: { id: number };
  Discography: undefined;
  Album: { id: number };
  Timeline: undefined;
  FreddieTribute: undefined;
  FanPerformances: undefined;
  FanPerformanceDetail: { id: string };
  Story: { id: number };
  AboutArchive: undefined;
  Search: undefined;
};

export type ForumStackParamList = {
  ForumIndex: undefined;
  Category: { id: number; name?: string };
  Thread: { id: number | string; title?: string };
  Composer: {
    threadId?: number;
    threadTitle?: string;
    categoryId?: number;
    categoryName?: string;
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

declare global {
  namespace ReactNavigation {
    interface RootParamList extends RootTabParamList {}
  }
}
