import { writeAsStringAsync } from 'expo-file-system/legacy';
import { cacheFileName, writeCachedLocalFile } from './writeCachedFile';

const writeAsString = writeAsStringAsync as jest.MockedFunction<typeof writeAsStringAsync>;

describe('writeCachedLocalFile', () => {
  it('writes the real file name under the cache directory as file://', async () => {
    const uri = await writeCachedLocalFile('tour-poster.jpg', Uint8Array.from([1, 2, 3]));

    expect(uri).toBe('file:///cache/tour-poster.jpg');
    expect(uri.startsWith('file:')).toBe(true);
    expect(writeAsString).toHaveBeenCalledWith(
      'file:///cache/tour-poster.jpg',
      expect.any(String),
      expect.objectContaining({ encoding: 'base64' }),
    );
  });

  it('strips path components from the file name', () => {
    expect(cacheFileName('../../secret.jpg')).toBe('secret.jpg');
    expect(cacheFileName('')).toBe('attachment');
  });
});
