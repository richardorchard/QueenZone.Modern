import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { dark } from '../theme';
import { TodayScreen } from '../screens/archive/TodayScreen';
import { BiographyScreen } from '../screens/archive/BiographyScreen';
import { DiscographyScreen } from '../screens/archive/DiscographyScreen';
import { TimelineScreen } from '../screens/archive/TimelineScreen';
import { FreddieTributeScreen } from '../screens/archive/FreddieTributeScreen';
import { FanPerformancesScreen } from '../screens/archive/FanPerformancesScreen';
import { FanPerformanceDetailScreen } from '../screens/archive/FanPerformanceDetailScreen';
import { StoryScreen } from '../screens/archive/StoryScreen';
import { SearchScreen } from '../screens/archive/SearchScreen';
import { NewsIndexScreen } from '../screens/news/NewsIndexScreen';
import { NewsStoryScreen } from '../screens/news/NewsStoryScreen';
import { PhotosScreen } from '../screens/photos/PhotosScreen';
import { PhotoViewerScreen } from '../screens/photos/PhotoViewerScreen';
import { PhotoSubmitScreen } from '../screens/photos/PhotoSubmitScreen';
import { ForumScreen } from '../screens/forum/ForumScreen';
import { ThreadScreen } from '../screens/forum/ThreadScreen';
import { ComposerScreen } from '../screens/forum/ComposerScreen';
import { InboxScreen } from '../screens/messages/InboxScreen';
import { ConversationScreen } from '../screens/messages/ConversationScreen';
import { ComposeMessageScreen } from '../screens/messages/ComposeMessageScreen';
import { AccountScreen } from '../screens/account/AccountScreen';
import { HelpScreen } from '../screens/account/HelpScreen';
import { SignInScreen } from '../screens/account/SignInScreen';
import { ProfileScreen } from '../screens/account/ProfileScreen';
import { SettingsScreen } from '../screens/account/SettingsScreen';
import type {
  ArchiveStackParamList,
  ForumStackParamList,
  MessagesStackParamList,
  NewsStackParamList,
  PhotosStackParamList,
  YouStackParamList,
} from './types';

/** Dark-first stack chrome (matches ThemeProvider default until light mode is user-facing). */
const stackScreenOptions = {
  headerStyle: { backgroundColor: dark.surfacePage },
  headerTintColor: dark.accentPrimary,
  headerTitleStyle: { color: dark.textPrimary, fontWeight: '600' as const },
  headerShadowVisible: false,
  contentStyle: { backgroundColor: dark.surfacePage },
};

const Archive = createNativeStackNavigator<ArchiveStackParamList>();
const News = createNativeStackNavigator<NewsStackParamList>();
const Photos = createNativeStackNavigator<PhotosStackParamList>();
const Forum = createNativeStackNavigator<ForumStackParamList>();
const Messages = createNativeStackNavigator<MessagesStackParamList>();
const You = createNativeStackNavigator<YouStackParamList>();

export function ArchiveStack() {
  return (
    <Archive.Navigator screenOptions={stackScreenOptions}>
      <Archive.Screen name="Today" component={TodayScreen} options={{ headerShown: false }} />
      <Archive.Screen name="Biography" component={BiographyScreen} />
      <Archive.Screen name="Discography" component={DiscographyScreen} />
      <Archive.Screen name="Timeline" component={TimelineScreen} />
      <Archive.Screen name="FreddieTribute" component={FreddieTributeScreen} options={{ title: 'Freddie Tribute' }} />
      <Archive.Screen name="FanPerformances" component={FanPerformancesScreen} options={{ title: 'Fan performances' }} />
      <Archive.Screen
        name="FanPerformanceDetail"
        component={FanPerformanceDetailScreen}
        options={{ title: 'Fan performance' }}
      />
      <Archive.Screen name="Story" component={StoryScreen} />
      <Archive.Screen name="Search" component={SearchScreen} />
    </Archive.Navigator>
  );
}

export function NewsStack() {
  return (
    <News.Navigator screenOptions={stackScreenOptions}>
      <News.Screen name="NewsIndex" component={NewsIndexScreen} options={{ title: 'News', headerShown: false }} />
      <News.Screen name="Story" component={NewsStoryScreen} />
    </News.Navigator>
  );
}

export function PhotosStack() {
  return (
    <Photos.Navigator screenOptions={stackScreenOptions}>
      <Photos.Screen name="PhotoIndex" component={PhotosScreen} options={{ title: 'Photos', headerShown: false }} />
      <Photos.Screen name="PhotoViewer" component={PhotoViewerScreen} options={{ title: 'Photograph' }} />
      <Photos.Screen name="PhotoSubmit" component={PhotoSubmitScreen} options={{ title: 'Submit a photo' }} />
    </Photos.Navigator>
  );
}

export function ForumStack() {
  return (
    <Forum.Navigator screenOptions={stackScreenOptions}>
      <Forum.Screen name="ForumIndex" component={ForumScreen} options={{ title: 'Forum', headerShown: false }} />
      <Forum.Screen name="Thread" component={ThreadScreen} />
      <Forum.Screen name="Composer" component={ComposerScreen} options={{ title: 'Compose', presentation: 'modal' }} />
    </Forum.Navigator>
  );
}

export function MessagesStack() {
  return (
    <Messages.Navigator screenOptions={stackScreenOptions}>
      <Messages.Screen name="Inbox" component={InboxScreen} options={{ title: 'Messages', headerShown: false }} />
      <Messages.Screen name="Conversation" component={ConversationScreen} />
      <Messages.Screen name="ComposeMessage" component={ComposeMessageScreen} options={{ title: 'New message' }} />
    </Messages.Navigator>
  );
}

export function YouStack() {
  return (
    <You.Navigator screenOptions={stackScreenOptions}>
      <You.Screen name="Account" component={AccountScreen} options={{ headerShown: false }} />
      <You.Screen name="Help" component={HelpScreen} />
      <You.Screen name="SignIn" component={SignInScreen} options={{ title: 'Sign in' }} />
      <You.Screen name="Profile" component={ProfileScreen} />
      <You.Screen name="Settings" component={SettingsScreen} />
    </You.Navigator>
  );
}
