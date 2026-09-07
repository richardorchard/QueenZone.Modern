import { RefreshControl } from 'react-native';
import { fireEvent, screen } from '@testing-library/react-native';
import { renderWithProviders } from '../test/render';
import { dark, light, palette, ThemeProvider } from '../theme';
import { ThemedRefreshControl } from './ThemedRefreshControl';

function renderControl(preference: 'dark' | 'light', onRefresh = jest.fn(), refreshing = false) {
  renderWithProviders(
    <ThemeProvider preference={preference}>
      <ThemedRefreshControl refreshing={refreshing} onRefresh={onRefresh} />
    </ThemeProvider>,
    { navigation: false },
  );
  return { onRefresh, control: screen.UNSAFE_getByType(RefreshControl) };
}

describe('ThemedRefreshControl', () => {
  it('owns dark iOS tint and Android spinner colours that contrast on surfacePage', () => {
    const { control } = renderControl('dark', jest.fn(), true);
    expect(control.props.refreshing).toBe(true);
    expect(control.props.tintColor).toBe(dark.accentPrimary);
    expect(control.props.colors).toEqual([dark.accentPrimary]);
    expect(control.props.progressBackgroundColor).toBe(palette.grey800);
    expect(control.props.progressBackgroundColor).not.toBe(dark.surfacePage);
  });

  it('owns light iOS tint and Android spinner colours that contrast on surfacePage', () => {
    const { control } = renderControl('light');
    expect(control.props.tintColor).toBe(light.accentPrimary);
    expect(control.props.colors).toEqual([light.accentPrimary]);
    expect(control.props.progressBackgroundColor).toBe(palette.grey100);
    expect(control.props.progressBackgroundColor).not.toBe(light.surfacePage);
  });

  it('forwards refreshing and onRefresh without changing epoch behaviour', () => {
    const onRefresh = jest.fn();
    const { control } = renderControl('dark', onRefresh, false);
    expect(control.props.refreshing).toBe(false);
    fireEvent(control, 'refresh');
    expect(onRefresh).toHaveBeenCalledTimes(1);
  });
});
