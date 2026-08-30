import { screen, userEvent } from '@testing-library/react-native';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { ArchiveHubScreen } from './ArchiveHubScreen';

describe('ArchiveHubScreen', () => {
  it('labels the articles destination Articles and opens the Articles route', async () => {
    const user = userEvent.setup();
    const navigation = fakeNavigation();
    renderWithProviders(
      <ArchiveHubScreen
        navigation={navigation as never}
        route={{ key: 'archive', name: 'ArchiveHub' } as never}
      />,
      { navigation: false },
    );
    await flushVirtualizedList();
    expect(screen.getByText('Articles')).toBeOnTheScreen();
    expect(screen.queryByText('Stories')).toBeNull();

    await user.press(screen.getByRole('button', { name: /Long-form\. Articles\./ }));
    expect(navigation.navigate).toHaveBeenCalledWith('Articles');
  });

  it('opens the Trivia route from the Queen facts row', async () => {
    const user = userEvent.setup();
    const navigation = fakeNavigation();
    renderWithProviders(
      <ArchiveHubScreen
        navigation={navigation as never}
        route={{ key: 'archive', name: 'ArchiveHub' } as never}
      />,
      { navigation: false },
    );
    await flushVirtualizedList();

    await user.press(screen.getByRole('button', { name: /Queen facts\. Trivia\./ }));
    expect(navigation.navigate).toHaveBeenCalledWith('Trivia');
  });
});
