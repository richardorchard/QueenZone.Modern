import { screen, userEvent } from '@testing-library/react-native';
import { HeaderBackButton, HeaderCloseButton, NewsIndexHeaderRight } from './headerButtons';
import { testIds } from '../test/testIds';
import { renderWithProviders } from '../test/render';

const mockOpenSuggestNews = jest.fn();

jest.mock('../share/news/NewsShare', () => ({
  openSuggestNews: (...args: unknown[]) => mockOpenSuggestNews(...args),
}));

describe('header dismiss controls', () => {
  it('exposes a tappable Profile back control', async () => {
    const onPress = jest.fn();
    const user = userEvent.setup();
    renderWithProviders(<HeaderBackButton testID={testIds.profileBack} onPress={onPress} />, {
      navigation: false,
    });
    await user.press(screen.getByTestId(testIds.profileBack));
    expect(onPress).toHaveBeenCalledTimes(1);
    expect(screen.getByLabelText('Back')).toBeOnTheScreen();
  });

  it('exposes a tappable Sign in close control', async () => {
    const onPress = jest.fn();
    const user = userEvent.setup();
    renderWithProviders(<HeaderCloseButton testID={testIds.signInClose} onPress={onPress} />, {
      navigation: false,
    });
    await user.press(screen.getByTestId(testIds.signInClose));
    expect(onPress).toHaveBeenCalledTimes(1);
    expect(screen.getByLabelText('Close')).toBeOnTheScreen();
  });

  it('opens Suggest news from the News header', async () => {
    const navigation = { navigate: jest.fn() };
    const user = userEvent.setup();
    renderWithProviders(<NewsIndexHeaderRight navigation={navigation as never} />, {
      navigation: false,
    });
    await user.press(screen.getByTestId(testIds.newsSuggest));
    expect(mockOpenSuggestNews).toHaveBeenCalledWith(navigation);
  });
});
