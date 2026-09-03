import { Platform } from 'react-native';
import { pressedOpacity, pressedStyle } from './press';

describe('pressedOpacity', () => {
  const originalOs = Platform.OS;

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: originalOs });
  });

  it('is the number pressedStyle applies on iOS', () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'ios' });
    expect(pressedOpacity).toBe(0.85);
    expect(pressedStyle({ pressed: true })).toEqual([{ opacity: pressedOpacity }]);
  });

  it('does not apply opacity when not pressed', () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'ios' });
    expect(pressedStyle({ pressed: false })).toBeNull();
  });
});
