import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { readFileSync } from 'node:fs';

const moduleDir = new URL('../../modules/queenzone-wallpaper/', import.meta.url);

describe('Android SET_WALLPAPER permission (#1409)', () => {
  it('declares only SET_WALLPAPER on the WallpaperManager module', () => {
    const manifest = readFileSync(new URL('android/src/main/AndroidManifest.xml', moduleDir), 'utf8');
    assert.match(manifest, /android\.permission\.SET_WALLPAPER/);
    assert.doesNotMatch(manifest, /READ_MEDIA_|READ_EXTERNAL_STORAGE|WRITE_EXTERNAL_STORAGE/);
  });

  it('keeps the Expo module Android-only so iOS cannot claim a set-wallpaper API', () => {
    const config = JSON.parse(readFileSync(new URL('expo-module.config.json', moduleDir), 'utf8')) as {
      platforms: string[];
      android: { modules: string[] };
    };
    assert.deepEqual(config.platforms, ['android']);
    assert.ok(config.android.modules.includes('org.queenzone.modules.wallpaper.QueenZoneWallpaperModule'));
  });

  it('uses FLAG_SYSTEM, FLAG_LOCK, or both in WallpaperManager.setStream', () => {
    const kotlin = readFileSync(
      new URL(
        'android/src/main/java/org/queenzone/modules/wallpaper/QueenZoneWallpaperModule.kt',
        moduleDir,
      ),
      'utf8',
    );
    assert.match(kotlin, /WallpaperManager\.FLAG_SYSTEM/);
    assert.match(kotlin, /WallpaperManager\.FLAG_LOCK/);
    assert.match(kotlin, /manager\.setStream/);
    assert.match(kotlin, /"home" -> WallpaperManager\.FLAG_SYSTEM/);
    assert.match(kotlin, /"lock" -> WallpaperManager\.FLAG_LOCK/);
    assert.match(kotlin, /"both" -> WallpaperManager\.FLAG_SYSTEM or WallpaperManager\.FLAG_LOCK/);
  });
});
