import { act, waitFor } from '@testing-library/react-native';
import * as Linking from 'expo-linking';
import { WidgetLinkBridge } from './WidgetLinkBridge';
import { renderWithProviders } from '../test/render';

const mockNavigate = jest.fn();

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({ navigate: mockNavigate }),
  };
});

jest.mock('expo-linking', () => ({
  getInitialURL: jest.fn(async () => null),
  addEventListener: jest.fn(() => ({ remove: jest.fn() })),
}));

const getInitialURL = Linking.getInitialURL as jest.MockedFunction<typeof Linking.getInitialURL>;
const addEventListener = Linking.addEventListener as jest.MockedFunction<typeof Linking.addEventListener>;

describe('WidgetLinkBridge', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    getInitialURL.mockReset();
    addEventListener.mockReset();
    getInitialURL.mockResolvedValue(null);
    addEventListener.mockReturnValue({ remove: jest.fn() } as never);
  });

  it('opens Timeline when launched from the widget URL', async () => {
    getInitialURL.mockResolvedValue('queenzone://timeline');
    renderWithProviders(<WidgetLinkBridge />);

    await waitFor(() =>
      expect(mockNavigate).toHaveBeenCalledWith('Tabs', {
        screen: 'ArchiveTab',
        params: { screen: 'Timeline', params: {}, initial: false },
      }),
    );
  });

  it('opens Timeline from a later url event', async () => {
    let handler: ((event: { url: string }) => void) | undefined;
    addEventListener.mockImplementation((_type, next) => {
      handler = next as (event: { url: string }) => void;
      return { remove: jest.fn() } as never;
    });

    renderWithProviders(<WidgetLinkBridge />);
    await waitFor(() => expect(addEventListener).toHaveBeenCalled());

    await act(async () => {
      handler?.({ url: 'queenzone://timeline' });
    });

    expect(mockNavigate).toHaveBeenCalledWith('Tabs', {
      screen: 'ArchiveTab',
      params: { screen: 'Timeline', params: {}, initial: false },
    });
  });

  it('ignores unrelated deep links', async () => {
    getInitialURL.mockResolvedValue('queenzone://forum');
    renderWithProviders(<WidgetLinkBridge />);
    await waitFor(() => expect(getInitialURL).toHaveBeenCalled());
    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
