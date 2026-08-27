import { getFocusedRouteNameFromRoute, type RouteProp } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Archive, Camera, House, MessageSquare, Newspaper, type LucideIcon } from 'lucide-react-native';
import { Platform, View } from 'react-native';
import { SignInScreen } from '../screens/account/SignInScreen';
import { testIds } from '../test/testIds';
import { useTheme, type ColorScheme } from '../theme';
import { NotificationBridge } from '../notifications/NotificationBridge';
import { NewsShareBridge } from '../share/news/NewsShare';
import { WidgetLinkBridge } from '../widgets/WidgetLinkBridge';
import { HeaderCloseButton } from './headerButtons';
import { ArchiveStack, ForumStack, HomeStack, NewsStack, PhotosStack, stackScreenOptions } from './stacks';
import type { RootStackParamList, RootTabParamList } from './types';
import { shouldHideTabBar } from './visibility';

const Tab = createBottomTabNavigator<RootTabParamList>();
const RootStack = createNativeStackNavigator<RootStackParamList>();

function TabGlyph({
  Icon,
  color,
  focused,
}: {
  Icon: LucideIcon;
  color: string;
  focused: boolean;
}) {
  const { c, chrome } = useTheme();
  const platform = Platform.OS === 'android' ? chrome.android : chrome.ios;
  const icon = <Icon color={color} size={platform.tabIcon} strokeWidth={1.5} />;

  if (platform.tabActiveStyle !== 'pill') {
    return icon;
  }

  return (
    <View
      style={{
        width: 64,
        height: 32,
        borderRadius: 16,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: focused ? c.accentTintWeak : 'transparent',
      }}
    >
      {icon}
    </View>
  );
}

function tabIcon(Icon: LucideIcon) {
  return function TabIcon({ color, focused }: { color: string; focused: boolean }) {
    return <TabGlyph Icon={Icon} color={color} focused={focused} />;
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

function MainTabs() {
  const { c, chrome } = useTheme();
  const platformChrome = Platform.OS === 'android' ? chrome.android : chrome.ios;

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: c.accentPrimary,
        tabBarInactiveTintColor: c.textMuted,
        tabBarStyle: tabBarStyleFor(c, false),
        tabBarLabelStyle: {
          fontSize: platformChrome.tabLabel,
          letterSpacing: 0.4,
        },
        tabBarHideOnKeyboard: true,
      }}
    >
      <Tab.Screen
        name="HomeTab"
        component={HomeStack}
        options={({ route }) => ({
          title: 'Home',
          tabBarAccessibilityLabel: 'Home',
          tabBarButtonTestID: testIds.tabHome,
          tabBarIcon: tabIcon(House),
          ...hideTabBarIfDetail(c, route, 'Home'),
        })}
        listeners={reselectRoot('HomeTab', 'Home')}
      />
      <Tab.Screen
        name="NewsTab"
        component={NewsStack}
        options={({ route }) => ({
          title: 'News',
          tabBarAccessibilityLabel: 'News',
          tabBarButtonTestID: testIds.tabNews,
          tabBarIcon: tabIcon(Newspaper),
          ...hideTabBarIfDetail(c, route, 'NewsIndex'),
        })}
        listeners={reselectRoot('NewsTab', 'NewsIndex')}
      />
      <Tab.Screen
        name="PhotosTab"
        component={PhotosStack}
        options={({ route }) => ({
          title: 'Photography',
          tabBarAccessibilityLabel: 'Photography',
          tabBarButtonTestID: testIds.tabPhotos,
          tabBarIcon: tabIcon(Camera),
          tabBarLabelStyle: {
            fontSize: platformChrome.tabLabel,
            letterSpacing: 0.2,
          },
          ...hideTabBarIfDetail(c, route, 'PhotoIndex'),
        })}
        listeners={reselectRoot('PhotosTab', 'PhotoIndex')}
      />
      <Tab.Screen
        name="ArchiveTab"
        component={ArchiveStack}
        options={({ route }) => ({
          title: 'Archive',
          tabBarAccessibilityLabel: 'Archive',
          tabBarButtonTestID: testIds.tabArchive,
          tabBarIcon: tabIcon(Archive),
          ...hideTabBarIfDetail(c, route, 'ArchiveHub'),
        })}
        listeners={reselectRoot('ArchiveTab', 'ArchiveHub')}
      />
      <Tab.Screen
        name="ForumTab"
        component={ForumStack}
        options={({ route }) => ({
          title: 'Forum',
          tabBarAccessibilityLabel: 'Forum',
          tabBarButtonTestID: testIds.tabForum,
          tabBarIcon: tabIcon(MessageSquare),
          ...hideTabBarIfDetail(c, route, 'ForumIndex'),
        })}
        listeners={reselectRoot('ForumTab', 'ForumIndex')}
      />
    </Tab.Navigator>
  );
}

export function RootNavigator() {
  return (
    <View style={{ flex: 1 }}>
      <RootStack.Navigator screenOptions={{ headerShown: false }}>
        <RootStack.Screen name="Tabs" component={MainTabs} />
        <RootStack.Screen
          name="SignIn"
          component={SignInScreen}
          options={({ navigation }) => ({
            ...stackScreenOptions,
            headerShown: true,
            title: 'Sign in',
            presentation: 'fullScreenModal',
            headerLeft: () => (
              <HeaderCloseButton testID={testIds.signInClose} onPress={() => navigation.goBack()} />
            ),
          })}
        />
      </RootStack.Navigator>
      <NotificationBridge />
      <NewsShareBridge />
      <WidgetLinkBridge />
    </View>
  );
}
