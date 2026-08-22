import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { dark } from '../theme';
import { HomeScreen } from '../screens/home/HomeScreen';

import { ProfileScreen } from '../screens/account/ProfileScreen';
import { SettingsScreen } from '../screens/account/SettingsScreen';
import { SignInScreen } from '../screens/account/SignInScreen';
import { HelpScreen } from '../screens/account/HelpScreen';
import { SavedListScreen } from '../screens/account/SavedListScreen';
import { InboxScreen } from '../screens/messages/InboxScreen';
import { ConversationScreen } from '../screens/messages/ConversationScreen';
import { ComposeMessageScreen } from '../screens/messages/ComposeMessageScreen';
import { ArchiveHubScreen } from '../screens/archive/ArchiveHubScreen';
import { StoriesIndexScreen } from '../screens/archive/StoriesIndexScreen';
import { BiographyScreen } from '../screens/archive/BiographyScreen';
import { BiographyChapterScreen } from '../screens/archive/BiographyChapterScreen';
import { DiscographyScreen } from '../screens/archive/DiscographyScreen';
import { AlbumScreen } from '../screens/archive/AlbumScreen';
import { TimelineScreen } from '../screens/archive/TimelineScreen';
import { FreddieTributeScreen } from '../screens/archive/FreddieTributeScreen';
import { FanPerformancesScreen } from '../screens/archive/FanPerformancesScreen';
import { FanPerformanceDetailScreen } from '../screens/archive/FanPerformanceDetailScreen';
import { StoryScreen } from '../screens/archive/StoryScreen';
import { AboutArchiveScreen } from '../screens/archive/AboutArchiveScreen';
import { SearchRouteScreen } from '../screens/archive/SearchScreen';
import { NewsIndexScreen } from '../screens/news/NewsIndexScreen';
import { NewsStoryScreen } from '../screens/news/NewsStoryScreen';
import { PhotosScreen } from '../screens/photos/PhotosScreen';
import { PhotoViewerScreen } from '../screens/photos/PhotoViewerScreen';
import { PhotoSubmitScreen } from '../screens/photos/PhotoSubmitScreen';
import { ForumScreen } from '../screens/forum/ForumScreen';
import { ThreadScreen } from '../screens/forum/ThreadScreen';
import { ComposerScreen } from '../screens/forum/ComposerScreen';
import { ForumHeaderRight, SearchHeaderButton } from './headerButtons';
import type {
  ArchiveStackParamList,
  ForumStackParamList,
  HomeStackParamList,
  NewsStackParamList,
  PhotosStackParamList,
} from './types';

const stackScreenOptions = {
  headerStyle: { backgroundColor: dark.surfacePage },
  headerTintColor: dark.accentPrimary,
  headerTitleStyle: { color: dark.textPrimary, fontWeight: '600' as const },
  headerShadowVisible: false,
  contentStyle: { backgroundColor: dark.surfacePage },
};

const Home = createNativeStackNavigator<HomeStackParamList>();
const News = createNativeStackNavigator<NewsStackParamList>();
const Photos = createNativeStackNavigator<PhotosStackParamList>();
const Archive = createNativeStackNavigator<ArchiveStackParamList>();
const Forum = createNativeStackNavigator<ForumStackParamList>();

export function HomeStack() {
  return (
    <Home.Navigator screenOptions={stackScreenOptions}>
      <Home.Screen name="Home" component={HomeScreen} options={{ headerShown: false }} />
      <Home.Screen name="Search" component={SearchRouteScreen} />
      <Home.Screen name="Profile" component={ProfileScreen} />
      <Home.Screen name="Settings" component={SettingsScreen} />
      <Home.Screen name="SignIn" component={SignInScreen} options={{ title: 'Sign in' }} />
      <Home.Screen name="Help" component={HelpScreen} />
      <Home.Screen name="Inbox" component={InboxScreen} options={{ title: 'Messages' }} />
      <Home.Screen name="Conversation" component={ConversationScreen} />
      <Home.Screen name="ComposeMessage" component={ComposeMessageScreen} options={{ title: 'New message' }} />
      <Home.Screen name="SavedList" component={SavedListScreen} options={{ title: 'Library' }} />
    </Home.Navigator>
  );
}

export function NewsStack() {
  return (
    <News.Navigator screenOptions={stackScreenOptions}>
      <News.Screen
        name="NewsIndex"
        component={NewsIndexScreen}
        options={({ navigation }) => ({
          title: 'News',
          headerRight: () => <SearchHeaderButton onPress={() => navigation.navigate('Search')} />,
        })}
      />
      <News.Screen name="Story" component={NewsStoryScreen} />
      <News.Screen name="Search" component={SearchRouteScreen} />
    </News.Navigator>
  );
}

export function PhotosStack() {
  return (
    <Photos.Navigator screenOptions={stackScreenOptions}>
      <Photos.Screen
        name="PhotoIndex"
        component={PhotosScreen}
        options={({ navigation }) => ({
          title: 'Photography',
          headerRight: () => <SearchHeaderButton onPress={() => navigation.navigate('Search')} />,
        })}
      />
      <Photos.Screen
        name="PhotoViewer"
        component={PhotoViewerScreen}
        options={{ headerShown: false, title: 'Photograph' }}
      />
      <Photos.Screen name="PhotoSubmit" component={PhotoSubmitScreen} options={{ title: 'Submit a photo' }} />
      <Photos.Screen name="Search" component={SearchRouteScreen} />
    </Photos.Navigator>
  );
}

export function ArchiveStack() {
  return (
    <Archive.Navigator screenOptions={stackScreenOptions}>
      <Archive.Screen
        name="ArchiveHub"
        component={ArchiveHubScreen}
        options={({ navigation }) => ({
          title: 'Archive',
          headerRight: () => <SearchHeaderButton onPress={() => navigation.navigate('Search')} />,
        })}
      />
      <Archive.Screen name="Stories" component={StoriesIndexScreen} />
      <Archive.Screen name="Biography" component={BiographyScreen} />
      <Archive.Screen name="BiographyChapter" component={BiographyChapterScreen} options={{ title: 'Chapter' }} />
      <Archive.Screen name="Discography" component={DiscographyScreen} />
      <Archive.Screen name="Album" component={AlbumScreen} options={{ title: 'Album' }} />
      <Archive.Screen name="Timeline" component={TimelineScreen} />
      <Archive.Screen
        name="FreddieTribute"
        component={FreddieTributeScreen}
        options={{ title: 'Freddie Tribute' }}
      />
      <Archive.Screen
        name="FanPerformances"
        component={FanPerformancesScreen}
        options={{ title: 'Fan performances' }}
      />
      <Archive.Screen
        name="FanPerformanceDetail"
        component={FanPerformanceDetailScreen}
        options={{ title: 'Fan performance' }}
      />
      <Archive.Screen name="Story" component={StoryScreen} />
      <Archive.Screen
        name="AboutArchive"
        component={AboutArchiveScreen}
        options={{ title: 'The archive' }}
      />
      <Archive.Screen name="Search" component={SearchRouteScreen} />
    </Archive.Navigator>
  );
}

export function ForumStack() {
  return (
    <Forum.Navigator screenOptions={stackScreenOptions}>
      <Forum.Screen
        name="ForumIndex"
        component={ForumScreen}
        options={({ navigation }) => ({
          title: 'Forum',
          headerRight: () => (
            <ForumHeaderRight
              onSearch={() => navigation.navigate('Search')}
              onCompose={() => navigation.navigate('Composer', {})}
            />
          ),
        })}
      />
      <Forum.Screen name="Thread" component={ThreadScreen} />
      <Forum.Screen name="Composer" component={ComposerScreen} options={{ title: 'Compose', presentation: 'modal' }} />
      <Forum.Screen name="Search" component={SearchRouteScreen} />
    </Forum.Navigator>
  );
}
