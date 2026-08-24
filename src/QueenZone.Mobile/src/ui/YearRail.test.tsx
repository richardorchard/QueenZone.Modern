import { screen, userEvent } from '@testing-library/react-native';
import { renderWithProviders } from '../test/render';
import { YearRail, type YearRailOption } from './YearRail';

const options: YearRailOption[] = [
  { label: 'ALL', decadeStart: null },
  { label: '2020s', decadeStart: 2020 },
  { label: '2010s', decadeStart: 2010 },
  { label: '2000s', decadeStart: 2000 },
];

describe('YearRail', () => {
  it('renders a tappable, labelled button per option', () => {
    renderWithProviders(<YearRail options={options} value={options[0]} onChange={jest.fn()} />, {
      navigation: false,
    });

    for (const option of options) {
      expect(screen.getByRole('button', { name: `Jump to ${option.label}` })).toBeOnTheScreen();
    }
  });

  it('marks the current value as selected', () => {
    renderWithProviders(<YearRail options={options} value={options[2]} onChange={jest.fn()} />, {
      navigation: false,
    });

    expect(screen.getByRole('button', { name: 'Jump to 2010s', selected: true })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Jump to ALL', selected: false })).toBeOnTheScreen();
  });

  it('calls onChange with the tapped option, without a drag gesture', async () => {
    const onChange = jest.fn();
    renderWithProviders(<YearRail options={options} value={options[0]} onChange={onChange} />, {
      navigation: false,
    });

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Jump to 2000s' }));

    expect(onChange).toHaveBeenCalledWith({ label: '2000s', decadeStart: 2000 });
  });
});
