import { screen } from '@testing-library/react-native';
import { reselectRoot, RootNavigator } from './RootNavigator';
import { createMockSession } from '../test/mockSession';
import { renderWithProviders } from '../test/render';

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

  it('always pops Archive to ArchiveHub, even when the tab is not focused', () => {
    const navigate = jest.fn();
    const listeners = reselectRoot('ArchiveTab', 'ArchiveHub', { always: true })({
      navigation: { isFocused: () => false, navigate },
    });

    listeners.tabPress();

    expect(navigate).toHaveBeenCalledWith('ArchiveTab', { screen: 'ArchiveHub' });
  });

  it('keeps News, Photos, and Forum reselect behind the focused-tab guard', () => {
    const navigate = jest.fn();
    for (const [tab, screen] of [
      ['NewsTab', 'NewsIndex'],
      ['PhotosTab', 'PhotoIndex'],
      ['ForumTab', 'ForumIndex'],
    ] as const) {
      const listeners = reselectRoot(tab, screen)({
        navigation: { isFocused: () => false, navigate },
      });
      listeners.tabPress();
    }

    expect(navigate).not.toHaveBeenCalled();

    const newsFocused = reselectRoot('NewsTab', 'NewsIndex')({
      navigation: { isFocused: () => true, navigate },
    });
    newsFocused.tabPress();
    expect(navigate).toHaveBeenCalledWith('NewsTab', { screen: 'NewsIndex' });
  });
});
