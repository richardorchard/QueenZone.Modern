import { Asset } from 'expo-asset';
import { lockScreenArtworkModule, resolveLockScreenArtworkUrl } from './lockScreenArtwork';

const bundledIcon = require('../../assets/icon.png') as number;

jest.mock('expo-asset', () => ({
  Asset: {
    fromModule: jest.fn(),
  },
}));

describe('lockScreenArtwork', () => {
  const fromModule = Asset.fromModule as jest.MockedFunction<typeof Asset.fromModule>;

  it('resolves the bundled Q icon to a local file URI', async () => {
    const downloadAsync = jest.fn();
    fromModule.mockReturnValue({
      localUri: 'file:///app/assets/icon.png',
      uri: 'https://should-not-use.example/icon.png',
      downloadAsync,
    } as unknown as Asset);

    await expect(resolveLockScreenArtworkUrl()).resolves.toBe('file:///app/assets/icon.png');
    expect(fromModule).toHaveBeenCalledWith(bundledIcon);
    expect(lockScreenArtworkModule).toBe(bundledIcon);
    expect(downloadAsync).not.toHaveBeenCalled();
  });

  it('downloads when localUri is missing and never falls back to a network URI', async () => {
    const asset = {
      localUri: undefined as string | undefined,
      uri: 'http://localhost:8081/assets/icon.png',
      downloadAsync: jest.fn(async () => {
        asset.localUri = 'file:///cache/icon.png';
      }),
    };
    fromModule.mockReturnValue(asset as unknown as Asset);

    await expect(resolveLockScreenArtworkUrl()).resolves.toBe('file:///cache/icon.png');
    expect(asset.downloadAsync).toHaveBeenCalled();
  });

  it('omits artwork when download leaves only a remote URI', async () => {
    fromModule.mockReturnValue({
      localUri: undefined,
      uri: 'https://cdn.example/icon.png',
      downloadAsync: jest.fn(async () => {}),
    } as unknown as Asset);

    await expect(resolveLockScreenArtworkUrl()).resolves.toBeUndefined();
  });
});
