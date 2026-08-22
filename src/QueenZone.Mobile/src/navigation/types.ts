import type { NavigatorScreenParams } from '@react-navigation/native';

export type ArchiveStackParamList = {
  Today: undefined;
  Biography: undefined;
  Discography: undefined;
  Timeline: undefined;
  FreddieTribute: undefined;
  FanPerformances: undefined;
  FanPerformanceDetail: { id: string };
  Story: { id: string };
  Search: undefined;
};

export type NewsStackParamList = {
  NewsIndex: undefined;
  Story: { id: string };
};

export type PhotosStackParamList = {
  PhotoIndex: undefined;
  PhotoViewer: { id: string };
  PhotoSubmit: undefined;
};

export type ForumStackParamList = {
  ForumIndex: undefined;
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
  Contact: undefined;
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
