import type { NavigatorScreenParams } from '@react-navigation/native';

export type ArchiveStackParamList = {
  Today: undefined;
  Biography: undefined;
  BiographyChapter: { id: number };
  Discography: undefined;
  Album: { id: number };
  Timeline: undefined;
  FreddieTribute: undefined;
  FanPerformances: undefined;
  FanPerformanceDetail: { id: string };
  Story: { id: number };
  Search: undefined;
};

export type NewsStackParamList = {
  NewsIndex: undefined;
  Story: { id: number };
};

export type PhotosStackParamList = {
  PhotoIndex: undefined;
  PhotoViewer: { id: string };
  PhotoSubmit: undefined;
};

export type ForumStackParamList = {
  ForumIndex: undefined;
  Category: { id: number; name?: string };
  Thread: { id: string };
  Composer: { threadId?: string };
};

export type MessagesStackParamList = {
  Inbox: undefined;
  Conversation: { id: string };
  ComposeMessage: undefined;
};

export type YouStackParamList = {
  Account: undefined;
  Help: undefined;
  SignIn: undefined;
  Profile: undefined;
  Settings: undefined;
};

export type RootTabParamList = {
  TodayTab: NavigatorScreenParams<ArchiveStackParamList>;
  NewsTab: NavigatorScreenParams<NewsStackParamList>;
  PhotosTab: NavigatorScreenParams<PhotosStackParamList>;
  ForumTab: NavigatorScreenParams<ForumStackParamList>;
  MessagesTab: NavigatorScreenParams<MessagesStackParamList>;
  YouTab: NavigatorScreenParams<YouStackParamList>;
};

declare global {
  namespace ReactNavigation {
    interface RootParamList extends RootTabParamList {}
  }
}
