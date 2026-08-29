import { screen } from '@testing-library/react-native';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { ArticlesIndexScreen } from './ArticlesIndexScreen';

describe('ArticlesIndexScreen', () => {
  it('titles the archive articles index Articles', async () => {
    renderWithProviders(
      <ArticlesIndexScreen
        navigation={fakeNavigation() as never}
        route={{ key: 'articles', name: 'Articles' } as never}
      />,
      { navigation: false },
    );
    await flushVirtualizedList();
    expect(screen.getByText('Articles')).toBeOnTheScreen();
    expect(screen.queryByText('Stories')).toBeNull();
  });
});
