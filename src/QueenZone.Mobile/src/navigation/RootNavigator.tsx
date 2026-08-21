import { getFocusedRouteNameFromRoute, type RouteProp } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import {
  BookOpen,
  Image,
  Mail,
  MessagesSquare,
  Newspaper,
  User,
  type LucideIcon,
} from 'lucide-react-native';
import { Platform } from 'react-native';
import { useSession } from '../session/SessionContext';
import { useTheme, type ColorScheme } from '../theme';
import { ArchiveStack, ForumStack, MessagesStack, NewsStack, PhotosStack, YouStack } from './stacks';
import type { RootTabParamList } from './types';
import { shouldHideTabBar } from './visibility';

const Tab = createBottomTabNavigator<RootTabParamList>();

function tabIcon(Icon: LucideIcon) {
  return function TabIcon({ color, size }: { color: string; size: number }) {
    return <Icon color={color} size={size} strokeWidth={1.5} />;
  };
}

function tabBarStyleFor(c: ColorScheme, hide: boolean) {
  if (hide) {
    return { display: 'none' as const };
  }
  return {
    backgroundColor: c.surfacePage,
    borderTopColor: c.hairline,
    borderTopWidth: 1,
  };
}

function hideTabBarIfDetail(
  c: ColorScheme,
  route: RouteProp<RootTabParamList, keyof RootTabParamList>,
  initialRouteName: string,
) {
  const focused = getFocusedRouteNameFromRoute(route) ?? initialRouteName;
  return {
    tabBarStyle: tabBarStyleFor(c, shouldHideTabBar(focused)),
  };
}

function reselectRoot(tabName: keyof RootTabParamList, screen: string) {
  return ({
    navigation,
  }: {
    navigation: {
      isFocused: () => boolean;
      navigate: (name: keyof RootTabParamList, params: { screen: string }) => void;
    };
  }) => ({
    tabPress: () => {
      if (navigation.isFocused()) {
        navigation.navigate(tabName, { screen });
      }
    },
  });
}

export function RootNavigator() {
  const { isSignedIn } = useSession();
  const { c, chrome } = useTheme();
  const platformChrome = Platform.OS === 'android' ? chrome.android : chrome.ios;

  return (
    <Tab.Navigator
      key={isSignedIn ? 'signed-in' : 'signed-out'}
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: c.accentPrimary,
        tabBarInactiveTintColor: c.textMuted,
        tabBarStyle: tabBarStyleFor(c, false),
        tabBarLabelStyle: { fontSize: platformChrome.tabLabel, letterSpacing: 0.4 },
        tabBarHideOnKeyboard: true,
      }}
    >
      <Tab.Screen
        name="TodayTab"
        component={ArchiveStack}
        options={({ route }) => ({
          title: 'Today',
          tabBarAccessibilityLabel: 'Today',
          tabBarIcon: tabIcon(BookOpen),
          ...hideTabBarIfDetail(c, route, 'Today'),
        })}
        listeners={reselectRoot('TodayTab', 'Today')}
      />
      <Tab.Screen
        name="NewsTab"
        component={NewsStack}
        options={({ route }) => ({
          title: 'News',
          tabBarAccessibilityLabel: 'News',
          tabBarIcon: tabIcon(Newspaper),
          ...hideTabBarIfDetail(c, route, 'NewsIndex'),
        })}
        listeners={reselectRoot('NewsTab', 'NewsIndex')}
      />
      <Tab.Screen
        name="PhotosTab"
        component={PhotosStack}
        options={({ route }) => ({
          title: 'Photos',
          tabBarAccessibilityLabel: 'Photos',
          tabBarIcon: tabIcon(Image),
          ...hideTabBarIfDetail(c, route, 'PhotoIndex'),
        })}
        listeners={reselectRoot('PhotosTab', 'PhotoIndex')}
      />
      <Tab.Screen
        name="ForumTab"
        component={ForumStack}
        options={({ route }) => ({
          title: 'Forum',
          tabBarAccessibilityLabel: 'Forum',
          tabBarIcon: tabIcon(MessagesSquare),
          ...hideTabBarIfDetail(c, route, 'ForumIndex'),
        })}
        listeners={reselectRoot('ForumTab', 'ForumIndex')}
      />
      {isSignedIn ? (
        <Tab.Screen
          name="MessagesTab"
          component={MessagesStack}
          options={({ route }) => ({
            title: 'Messages',
            tabBarAccessibilityLabel: 'Messages',
            tabBarIcon: tabIcon(Mail),
            ...hideTabBarIfDetail(c, route, 'Inbox'),
          })}
          listeners={reselectRoot('MessagesTab', 'Inbox')}
        />
      ) : null}
      <Tab.Screen
        name="YouTab"
        component={YouStack}
        options={({ route }) => ({
          title: 'You',
          tabBarAccessibilityLabel: 'You',
          tabBarIcon: tabIcon(User),
          ...hideTabBarIfDetail(c, route, 'Account'),
        })}
        listeners={reselectRoot('YouTab', 'Account')}
      />
    </Tab.Navigator>
  );
}
