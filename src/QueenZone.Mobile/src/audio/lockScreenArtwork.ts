import { Asset } from 'expo-asset';

/** Bundled Q app icon — one image for every fan-performance, never per-track art. */
export const lockScreenArtworkModule = require('../../assets/icon.png') as number;

/**
 * Resolve a file URI for expo-audio `artworkUrl`. Uses `expo-asset` so Android
 * release builds get `file://…` rather than a Metro http URL or a `require()`
 * module id (those fail native lock-screen artwork). Never returns http(s) or
 * blob URLs.
 */
export async function resolveLockScreenArtworkUrl(): Promise<string | undefined> {
  const asset = Asset.fromModule(lockScreenArtworkModule);
  if (!asset.localUri) {
    await asset.downloadAsync();
  }

  return asset.localUri ?? undefined;
}
