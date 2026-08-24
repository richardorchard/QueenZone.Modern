import { screen } from '@testing-library/react-native';
import { RootNavigator } from './RootNavigator';
import { createMockSession } from '../test/mockSession';
import { renderWithProviders } from '../test/render';

const mockSession = createMockSession();

jest.mock('../session/SessionContext', () => ({
  useSession: () => mockSession,
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
});
