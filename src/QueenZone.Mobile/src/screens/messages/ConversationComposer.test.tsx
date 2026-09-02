import { Platform } from 'react-native';
import { space } from '../../theme';
import { conversationComposerPaddingBottom } from './ConversationComposer';

describe('conversationComposerPaddingBottom', () => {
  const originalOs = Platform.OS;

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: originalOs });
  });

  it('uses the safe-area inset when the iOS keyboard is closed', () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'ios' });
    expect(conversationComposerPaddingBottom(34, false)).toBe(34);
    expect(conversationComposerPaddingBottom(0, false)).toBe(space.md);
  });

  it('uses space.md when the iOS keyboard is open', () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'ios' });
    expect(conversationComposerPaddingBottom(34, true)).toBe(space.md);
  });

  it('keeps the safe-area inset on Android even when the keyboard is open', () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'android' });
    expect(conversationComposerPaddingBottom(24, true)).toBe(24);
    expect(conversationComposerPaddingBottom(0, true)).toBe(space.md);
  });
});
