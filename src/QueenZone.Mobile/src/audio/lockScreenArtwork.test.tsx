import { Asset } from 'expo-asset';

const bundledIcon = require('../../assets/icon.png') as number;

jest.mock('expo-asset', () => ({
  Asset: {
    fromModule: jest.fn(),
  },
}));

describe('lockScreenArtwork', () => {
  const fromModule = Asset.fromModule as jest.MockedFunction<typeof Asset.fromModule>;

  beforeEach(() => {
    jest.resetModules();
  });

  it('resolves the bundled Q icon to a local file URI', async () => {
    fromModule.mockReturnValue({
      localUri: 'file:///app/assets/icon.png',
      uri: 'https://should-not-use.example/icon.png',
      downloadAsync: jest.fn(),
    } as unknown as Asset);

    const { lockScreenArtworkModule, resolveLockScreenArtworkUrl: resolve } =
      require('./lockScreenArtwork') as {
        resolveLockScreenArtworkUrl: () => Promise<string | undefined>;
        lockScreenArtworkModule: number;
      };

    await expect(resolve()).resolves.toBe('file:///app/assets/icon.png');
    expect(fromModule).toHaveBeenCalledWith(bundledIcon);
    expect(lockScreenArtworkModule).toBe(bundledIcon);
    expect(fromModule.mock.results[0]?.value.downloadAsync).not.toHaveBeenCalled();
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

    const { resolveLockScreenArtworkUrl: resolve } = require('./lockScreenArtwork') as {
      resolveLockScreenArtworkUrl: () => Promise<string | undefined>;
    };

    await expect(resolve()).resolves.toBe('file:///cache/icon.png');
    expect(asset.downloadAsync).toHaveBeenCalled();
  });

  it('omits artwork when download leaves only a remote URI', async () => {
    fromModule.mockReturnValue({
      localUri: undefined,
      uri: 'https://cdn.example/icon.png',
      downloadAsync: jest.fn(async () => {}),
    } as unknown as Asset);

    const { resolveLockScreenArtworkUrl: resolve } = require('./lockScreenArtwork') as {
      resolveLockScreenArtworkUrl: () => Promise<string | undefined>;
    };

    await expect(resolve()).resolves.toBeUndefined();
  });
});
