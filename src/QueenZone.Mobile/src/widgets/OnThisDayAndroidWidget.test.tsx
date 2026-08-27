import { screen } from '@testing-library/react-native';
import { OnThisDayAndroidWidget } from './OnThisDayAndroidWidget';
import { renderWithProviders } from '../test/render';

jest.mock('react-native-android-widget', () => {
  const { View, Text } = require('react-native') as typeof import('react-native');
  return {
    FlexWidget: View,
    TextWidget: ({ text }: { text: string }) => <Text>{text}</Text>,
  };
});

describe('OnThisDayAndroidWidget', () => {
  it('renders on-this-day and quote lines together', () => {
    renderWithProviders(
      <OnThisDayAndroidWidget
        formattedDate="30 June 1980"
        summary="Queen released The Game."
        quoteText="A kind of magic"
        quoteWhoSaid="Freddie Mercury"
      />,
      { navigation: false },
    );

    expect(screen.getByText('ON THIS DAY')).toBeOnTheScreen();
    expect(screen.getByText('30 June 1980: Queen released The Game.')).toBeOnTheScreen();
    expect(screen.getByText('“A kind of magic” — Freddie Mercury')).toBeOnTheScreen();
  });

  it('falls back to quote-only copy when there is no event', () => {
    renderWithProviders(
      <OnThisDayAndroidWidget quoteText="A kind of magic" quoteWhoSaid="Freddie Mercury" />,
      { navigation: false },
    );

    expect(screen.getByText('QUOTE OF THE DAY')).toBeOnTheScreen();
    expect(screen.getByText('“A kind of magic” — Freddie Mercury')).toBeOnTheScreen();
  });

  it('asks the member to open the app when both halves are missing', () => {
    renderWithProviders(<OnThisDayAndroidWidget />, { navigation: false });
    expect(screen.getByText("Open QueenZone to load today's story.")).toBeOnTheScreen();
  });
});
