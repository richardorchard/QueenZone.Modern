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
import { useSession } from '../session/SessionContext';
import { shellColors } from '../ui/shell';
import { ArchiveStack, ForumStack, MessagesStack, NewsStack, PhotosStack, YouStack } from './stacks';
import type { RootTabParamList } from './types';
import { shouldHideTabBar } from './visibility';

const Tab = createBottomTabNavigator<RootTabParamList>();

const tabBarBaseStyle = {
  backgroundColor: shellColors.page,
  borderTopColor: shellColors.hairline,
  borderTopWidth: 1,
};

function tabIcon(Icon: LucideIcon) {
  return function TabIcon({ color, size }: { color: string; size: number }) {
    return <Icon color={color} size={size} strokeWidth={1.5} />;
  };
}

function hideTabBarIfDetail(
  route: RouteProp<RootTabParamList, keyof RootTabParamList>,
  initialRouteName: string,
) {
  const focused = getFocusedRouteNameFromRoute(route) ?? initialRouteName;
  return {
    tabBarStyle: shouldHideTabBar(focused) ? { display: 'none' as const } : tabBarBaseStyle,
  };
}

function reselectRoot(tabName: keyof RootTabParamList, screen: string) {
  return ({
    navigation,
  }: {
    navigation: { isFocused: () => boolean; navigate: (name: keyof RootTabParamList, params: { screen: string }) => void };
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

  return (
    <Tab.Navigator
      key={isSignedIn ? 'signed-in' : 'signed-out'}
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: shellColors.accent,
        tabBarInactiveTintColor: shellColors.textMuted,
        tabBarStyle: tabBarBaseStyle,
        tabBarLabelStyle: { fontSize: 10, letterSpacing: 0.4 },
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
          ...hideTabBarIfDetail(route, 'Today'),
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
          ...hideTabBarIfDetail(route, 'NewsIndex'),
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
          ...hideTabBarIfDetail(route, 'PhotoIndex'),
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
          ...hideTabBarIfDetail(route, 'ForumIndex'),
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
            ...hideTabBarIfDetail(route, 'Inbox'),
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
          ...hideTabBarIfDetail(route, 'Account'),
        })}
        listeners={reselectRoot('YouTab', 'Account')}
      />
    </Tab.Navigator>
  );
}
