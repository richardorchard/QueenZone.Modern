import { screen } from '@testing-library/react-native';
import { OnThisDayAndroidWidget } from './OnThisDayAndroidWidget';
import { renderWithProviders } from '../test/render';
import { WIDGET_FACE_SLOT_MS } from './widgetCopy';

jest.mock('react-native-android-widget', () => {
  const { View, Text } = require('react-native') as typeof import('react-native');
  return {
    FlexWidget: View,
    OverlapWidget: View,
    ImageWidget: () => <View testID="widget-crest" />,
    TextWidget: ({ text }: { text: string }) => <Text>{text}</Text>,
  };
});

const bothHalves = {
  formattedDate: '30 June 1980',
  summary: 'Queen released The Game.',
  quoteText: 'A kind of magic',
  quoteWhoSaid: 'Freddie Mercury',
};

describe('OnThisDayAndroidWidget', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('shows only the on-this-day face in an even 4-hour slot', () => {
    jest.spyOn(Date, 'now').mockReturnValue(0);
    renderWithProviders(<OnThisDayAndroidWidget {...bothHalves} />, { navigation: false });

    expect(screen.getByText('ON THIS DAY')).toBeOnTheScreen();
    expect(screen.getByText('30 June 1980: Queen released The Game.')).toBeOnTheScreen();
    expect(screen.queryByText('QUEEN QUOTES')).toBeNull();
    expect(screen.queryByText('“A kind of magic” — Freddie Mercury')).toBeNull();
    expect(screen.getByTestId('widget-crest')).toBeOnTheScreen();
  });

  it('shows only the Queen Quotes face in an odd 4-hour slot', () => {
    jest.spyOn(Date, 'now').mockReturnValue(WIDGET_FACE_SLOT_MS);
    renderWithProviders(<OnThisDayAndroidWidget {...bothHalves} />, { navigation: false });

    expect(screen.getByText('QUEEN QUOTES')).toBeOnTheScreen();
    expect(screen.getByText('“A kind of magic” — Freddie Mercury')).toBeOnTheScreen();
    expect(screen.queryByText('ON THIS DAY')).toBeNull();
    expect(screen.queryByText('30 June 1980: Queen released The Game.')).toBeNull();
  });

  it('falls back to quote-only copy when there is no event', () => {
    renderWithProviders(
      <OnThisDayAndroidWidget quoteText="A kind of magic" quoteWhoSaid="Freddie Mercury" />,
      { navigation: false },
    );

    expect(screen.getByText('QUEEN QUOTES')).toBeOnTheScreen();
    expect(screen.getByText('“A kind of magic” — Freddie Mercury')).toBeOnTheScreen();
  });

  it('asks the member to open the app when both halves are missing', () => {
    renderWithProviders(<OnThisDayAndroidWidget />, { navigation: false });
    expect(screen.getByText('ON THIS DAY')).toBeOnTheScreen();
    expect(screen.getByText("Open QueenZone to load today's story.")).toBeOnTheScreen();
  });
});
