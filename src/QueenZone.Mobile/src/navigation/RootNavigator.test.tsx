import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createNavigationContainerRef, NavigationContainer } from '@react-navigation/native';
import { fireEvent, screen } from '@testing-library/react-native';
import { Pressable, Text } from 'react-native';
import { createMockSession } from '../test/mockSession';
import { renderWithProviders } from '../test/render';
import { nestedTabParams } from './nestedTab';
import { reselectRoot, RootNavigator } from './RootNavigator';

const mockSession = createMockSession();

jest.mock('../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../notifications/NotificationBridge', () => ({
  NotificationBridge: () => null,
}));

jest.mock('../share/news/NewsShare', () => ({
  NewsShareBridge: () => null,
}));

jest.mock('../widgets/WidgetLinkBridge', () => ({
  WidgetLinkBridge: () => null,
}));

jest.mock('./stacks', () => {
  const React = require('react');
  const { Text } = require('react-native');
  const stub = (label: string) => () => React.createElement(Text, null, label);
  return {
    HomeStack: stub('Home stack'),
    NewsStack: stub('News stack'),
    PhotosStack: stub('Photos stack'),
    ArchiveStack: stub('Archive stack'),
    ForumStack: stub('Forum stack'),
  };
});

describe('RootNavigator', () => {
  it('shows the five public tabs', () => {
    renderWithProviders(<RootNavigator />);
    for (const name of ['Home', 'News', 'Photography', 'Archive', 'Forum']) {
      expect(screen.getByLabelText(name)).toBeOnTheScreen();
    }
    expect(screen.getByText('Home stack')).toBeOnTheScreen();
  });

  it('pressing Archive after a widget day-face tap lands on ArchiveHub, not leftover Timeline', () => {
    const ref = createNavigationContainerRef();
    renderWithProviders(
      <NavigationContainer ref={ref}>
        <ArchiveTabRaceTabs />
      </NavigationContainer>,
      { navigation: false },
    );

    fireEvent.press(screen.getByRole('button', { name: 'Open widget event' }));
    expect(ref.getCurrentRoute()?.name).toBe('Timeline');

    fireEvent.press(screen.getByLabelText('Home'));
    expect(ref.getCurrentRoute()?.name).toBe('HomeTab');

    fireEvent.press(screen.getByLabelText('Archive'));
    expect(ref.getCurrentRoute()?.name).toBe('ArchiveHub');
  });

  it('pressing Archive after Home View timeline lands on ArchiveHub, not leftover Timeline', () => {
    const ref = createNavigationContainerRef();
    renderWithProviders(
      <NavigationContainer ref={ref}>
        <ArchiveTabRaceTabs />
      </NavigationContainer>,
      { navigation: false },
    );

    fireEvent.press(screen.getByRole('button', { name: 'View timeline' }));
    expect(ref.getCurrentRoute()?.name).toBe('Timeline');

    fireEvent.press(screen.getByLabelText('Home'));
    expect(ref.getCurrentRoute()?.name).toBe('HomeTab');

    fireEvent.press(screen.getByLabelText('Archive'));
    expect(ref.getCurrentRoute()?.name).toBe('ArchiveHub');
  });

  it('keeps News, Photos, and Forum reselect behind the focused-tab guard', () => {
    const navigate = jest.fn();
    const preventDefault = jest.fn();
    for (const [tab, screen] of [
      ['NewsTab', 'NewsIndex'],
      ['PhotosTab', 'PhotoIndex'],
      ['ForumTab', 'ForumIndex'],
    ] as const) {
      const listeners = reselectRoot(tab, screen)({
        navigation: { isFocused: () => false, navigate },
      });
      listeners.tabPress({ preventDefault });
    }

    expect(navigate).not.toHaveBeenCalled();
    expect(preventDefault).not.toHaveBeenCalled();

    const newsFocused = reselectRoot('NewsTab', 'NewsIndex')({
      navigation: { isFocused: () => true, navigate },
    });
    newsFocused.tabPress({ preventDefault });
    expect(navigate).toHaveBeenCalledWith('NewsTab', { screen: 'NewsIndex' });
    expect(preventDefault).not.toHaveBeenCalled();
  });
});

const Tab = createBottomTabNavigator();
const Archive = createNativeStackNavigator();

function ArchiveTabRaceTabs() {
  return (
    <Tab.Navigator>
      <Tab.Screen
        name="HomeTab"
        component={HomeTabRaceScreen}
        options={{ title: 'Home', tabBarAccessibilityLabel: 'Home' }}
      />
      <Tab.Screen
        name="ArchiveTab"
        component={ArchiveTabRaceStack}
        options={{ title: 'Archive', tabBarAccessibilityLabel: 'Archive' }}
        listeners={reselectRoot('ArchiveTab', 'ArchiveHub', { always: true })}
      />
    </Tab.Navigator>
  );
}

function HomeTabRaceScreen({
  navigation,
}: {
  navigation: {
    navigate: (
      name: 'ArchiveTab',
      params:
        | ReturnType<typeof nestedTabParams<'Timeline'>>
        | ReturnType<typeof nestedTabParams<'Timeline', { focusId: number }>>,
    ) => void;
  };
}) {
  return (
    <>
      <Text>Home screen</Text>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="View timeline"
        onPress={() => navigation.navigate('ArchiveTab', nestedTabParams('Timeline'))}
      >
        <Text>View timeline</Text>
      </Pressable>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Open widget event"
        onPress={() => navigation.navigate('ArchiveTab', nestedTabParams('Timeline', { focusId: 12 }))}
      >
        <Text>Open widget event</Text>
      </Pressable>
    </>
  );
}

function ArchiveTabRaceStack() {
  return (
    <Archive.Navigator>
      <Archive.Screen name="ArchiveHub" component={ArchiveHubRaceScreen} />
      <Archive.Screen name="Timeline" component={TimelineRaceScreen} />
    </Archive.Navigator>
  );
}

function ArchiveHubRaceScreen() {
  return <Text>Explore the archive</Text>;
}

function TimelineRaceScreen() {
  return <Text>Timeline section</Text>;
}
