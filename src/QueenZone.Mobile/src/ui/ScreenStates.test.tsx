import { screen, userEvent } from '@testing-library/react-native';
import { EmptyBlock, ErrorBlock, LoadingBlock } from './ScreenStates';
import { renderWithProviders } from '../test/render';

describe('ScreenStates', () => {
  it('exposes a labelled loading progressbar', () => {
    renderWithProviders(<LoadingBlock label="Loading news…" />, { navigation: false });
    expect(screen.getByLabelText('Loading news…')).toBeOnTheScreen();
  });

  it('shows error copy and invokes retry', async () => {
    const user = userEvent.setup();
    const onRetry = jest.fn();
    renderWithProviders(
      <ErrorBlock message="The server had a problem. Try again shortly." onRetry={onRetry} />,
      { navigation: false },
    );
    expect(screen.getByText('Unable to load')).toBeOnTheScreen();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    expect(onRetry).toHaveBeenCalled();
  });

  it('renders empty copy', () => {
    renderWithProviders(<EmptyBlock message="No news articles yet." />, { navigation: false });
    expect(screen.getByText('No news articles yet.')).toBeOnTheScreen();
  });
});
