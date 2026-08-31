jest.mock('./nativeUpload', () => ({
  shouldUseNativeMultipartUpload: () => true,
}));

// eslint-disable-next-line import/first -- Jest mock must hoist before the module under test.
import { appendUploadFile } from './uploadFile';

describe('appendUploadFile on native', () => {
  it('appends the React Native file part and does not fetch the URI', async () => {
    const fetchMock = jest.fn();
    global.fetch = fetchMock as unknown as typeof fetch;
    const append = jest.fn();

    await appendUploadFile({ append } as unknown as FormData, 'file', {
      uri: 'file:///tmp/avatar.jpg',
      name: 'avatar.jpg',
      type: 'image/jpeg',
    });

    expect(fetchMock).not.toHaveBeenCalled();
    expect(append).toHaveBeenCalledWith('file', {
      uri: 'file:///tmp/avatar.jpg',
      name: 'avatar.jpg',
      type: 'image/jpeg',
    });
  });
});
