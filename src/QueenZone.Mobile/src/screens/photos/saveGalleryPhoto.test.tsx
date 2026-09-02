import { Alert, Linking } from 'react-native';
import {
  getPermissionsAsync,
  requestPermissionsAsync,
  saveToLibraryAsync,
} from 'expo-media-library/legacy';
import { writeAsStringAsync } from 'expo-file-system/legacy';
import { saveToPhotosCopy } from '../../media/saveToPhotos';
import { galleryCacheFileName, saveGalleryPhoto, saveGalleryPhotoCopy } from './saveGalleryPhoto';

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();
const getPermissions = getPermissionsAsync as jest.MockedFunction<typeof getPermissionsAsync>;
const requestPermissions = requestPermissionsAsync as jest.MockedFunction<
  typeof requestPermissionsAsync
>;
const saveToLibrary = saveToLibraryAsync as jest.MockedFunction<typeof saveToLibraryAsync>;
const writeAsString = writeAsStringAsync as jest.MockedFunction<typeof writeAsStringAsync>;

const fullImage = 'https://cdn.queenzone.org/brian-may/img-101.jpg';
const thumbnail = 'https://cdn.queenzone.org/brian-may/img-101-t.jpg';

function okResponse(bytes: number[], url: string): Response {
  return {
    ok: true,
    status: 200,
    url,
    headers: {
      get: (name: string) => (name.toLowerCase() === 'content-type' ? 'image/jpeg' : null),
    },
    arrayBuffer: async () => Uint8Array.from(bytes).buffer,
  } as unknown as Response;
}

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
  getPermissions.mockResolvedValue({
    granted: true,
    canAskAgain: true,
    status: 'granted',
  } as never);
  requestPermissions.mockResolvedValue({
    granted: true,
    canAskAgain: true,
    status: 'granted',
  } as never);
  saveToLibrary.mockResolvedValue(undefined);
  writeAsString.mockResolvedValue(undefined);
  jest.spyOn(Alert, 'alert');
  jest.spyOn(Linking, 'openSettings').mockResolvedValue(undefined);
});

afterEach(() => {
  jest.restoreAllMocks();
});

describe('saveGalleryPhoto', () => {
  it('GETs the full CDN image with no Bearer and saves the cached file', async () => {
    fetchMock.mockResolvedValueOnce(okResponse([1, 2, 3], fullImage));

    await saveGalleryPhoto(fullImage);

    expect(fetchMock).toHaveBeenCalledWith(
      fullImage,
      expect.objectContaining({
        method: 'GET',
        redirect: 'follow',
        credentials: 'omit',
        headers: expect.not.objectContaining({
          Authorization: expect.anything(),
        }),
      }),
    );
    const headers = (fetchMock.mock.calls[0]?.[1] as RequestInit | undefined)?.headers as
      | Record<string, string>
      | undefined;
    expect(headers?.Authorization).toBeUndefined();
    expect(JSON.stringify(fetchMock.mock.calls[0]?.[1])).not.toMatch(/Bearer/i);
    expect(fetchMock).not.toHaveBeenCalledWith(thumbnail, expect.anything());
    expect(writeAsString).toHaveBeenCalledWith(
      'file:///cache/img-101.jpg',
      expect.any(String),
      expect.objectContaining({ encoding: 'base64' }),
    );
    expect(saveToLibrary).toHaveBeenCalledWith('file:///cache/img-101.jpg');
    expect(saveToLibrary.mock.calls[0]?.[0]).toMatch(/^file:/);
    expect(saveToLibrary.mock.calls[0]?.[0]).not.toMatch(/^https:/);
  });

  it('refuses a non-CDN URL without fetching', async () => {
    await expect(saveGalleryPhoto('https://www.queenzone.org/not-cdn.jpg')).rejects.toMatchObject({
      message: saveGalleryPhotoCopy.refused,
    });
    expect(fetchMock).not.toHaveBeenCalled();
    expect(saveToLibrary).not.toHaveBeenCalled();
  });

  it('refuses a non-CDN final URL after follow', async () => {
    fetchMock.mockResolvedValueOnce(
      okResponse([1], 'https://www.queenzone.org/files/img-101.jpg'),
    );

    await expect(saveGalleryPhoto(fullImage)).rejects.toMatchObject({
      message: saveGalleryPhotoCopy.refused,
    });
    expect(saveToLibrary).not.toHaveBeenCalled();
  });

  it('does not pass a remote https URI to MediaLibrary when the download fails', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: false,
      status: 404,
      url: fullImage,
      headers: { get: () => null },
      arrayBuffer: async () => new ArrayBuffer(0),
    } as unknown as Response);

    await expect(saveGalleryPhoto(fullImage)).rejects.toMatchObject({
      message: saveGalleryPhotoCopy.failed,
    });
    expect(saveToLibrary).not.toHaveBeenCalled();
  });

  it('prompts Settings when add-only permission is denied', async () => {
    fetchMock.mockResolvedValueOnce(okResponse([1, 2, 3], fullImage));
    getPermissions.mockResolvedValueOnce({
      granted: false,
      canAskAgain: false,
      status: 'denied',
    } as never);

    await expect(saveGalleryPhoto(fullImage)).rejects.toMatchObject({
      name: 'SaveToPhotosError',
      reason: 'permission-denied',
      message: saveToPhotosCopy.denied,
    });

    expect(saveToLibrary).not.toHaveBeenCalled();
    expect(Alert.alert).toHaveBeenCalledWith(
      saveToPhotosCopy.settingsTitle,
      saveToPhotosCopy.settingsBody,
      expect.arrayContaining([
        expect.objectContaining({ text: saveToPhotosCopy.cancel }),
        expect.objectContaining({ text: saveToPhotosCopy.settingsAction }),
      ]),
    );
  });

  it('falls back to the requested CDN URL when the response URL is empty', async () => {
    fetchMock.mockResolvedValueOnce(okResponse([1], ''));

    await saveGalleryPhoto(fullImage);

    expect(saveToLibrary).toHaveBeenCalledWith('file:///cache/img-101.jpg');
  });

  it('names the cache file from the CDN path', () => {
    expect(galleryCacheFileName(fullImage)).toBe('img-101.jpg');
    expect(galleryCacheFileName('not a url')).toBe('photograph.jpg');
  });
});
