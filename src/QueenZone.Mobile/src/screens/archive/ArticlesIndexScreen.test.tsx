import { screen } from '@testing-library/react-native';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { ArticlesIndexScreen } from './ArticlesIndexScreen';

describe('ArticlesIndexScreen', () => {
  it('titles the archive articles index Articles and shows the empty list', () => {
    const navigation = fakeNavigation();
    renderWithProviders(
      <ArticlesIndexScreen
        navigation={navigation as never}
        route={{ key: 'articles', name: 'Articles' } as never}
      />,
      { navigation: false },
    );
    expect(screen.getByText('Articles')).toBeOnTheScreen();
    expect(screen.getByText('No articles yet.')).toBeOnTheScreen();
    expect(screen.queryByText('Stories')).toBeNull();
    expect(screen.queryByText('The day Queen stole Live Aid')).toBeNull();
    expect(screen.queryByText('Inside the making of Bohemian Rhapsody')).toBeNull();
    expect(navigation.navigate).not.toHaveBeenCalled();
  });
});
