import * as Sharing from 'expo-sharing';
import { isShareCanceled, shareLocalFile } from './shareLocalFile';

const shareAsync = Sharing.shareAsync as jest.MockedFunction<typeof Sharing.shareAsync>;
const isAvailableAsync = Sharing.isAvailableAsync as jest.MockedFunction<
  typeof Sharing.isAvailableAsync
>;

beforeEach(() => {
  isAvailableAsync.mockResolvedValue(true);
  shareAsync.mockResolvedValue(undefined);
});

describe('shareLocalFile', () => {
  it('shares a file:// URI and never a data URI', async () => {
    await shareLocalFile('file:///cache/notes.pdf', 'application/pdf', 'notes.pdf');

    expect(shareAsync).toHaveBeenCalledWith('file:///cache/notes.pdf', {
      mimeType: 'application/pdf',
      dialogTitle: 'notes.pdf',
    });
    await expect(
      shareLocalFile('data:text/plain;base64,Zg==', 'text/plain', 'notes.txt'),
    ).rejects.toThrow('Unable to share this file.');
    expect(shareAsync).toHaveBeenCalledTimes(1);
  });

  it('treats user cancel as success', async () => {
    shareAsync.mockRejectedValueOnce(new Error('User cancelled sharing'));

    await expect(
      shareLocalFile('file:///cache/notes.pdf', 'application/pdf', 'notes.pdf'),
    ).resolves.toBeUndefined();
  });

  it('rethrows a share failure that is not cancel', async () => {
    shareAsync.mockRejectedValueOnce(new Error('share failed'));

    await expect(
      shareLocalFile('file:///cache/notes.pdf', 'application/pdf', 'notes.pdf'),
    ).rejects.toThrow('share failed');
  });
});

describe('isShareCanceled', () => {
  it('recognizes cancel and dismiss wording', () => {
    expect(isShareCanceled(new Error('User cancelled sharing'))).toBe(true);
    expect(isShareCanceled(new Error('ERR_SHARING_CANCELLED'))).toBe(true);
    expect(isShareCanceled(new Error('dismissed'))).toBe(true);
    expect(isShareCanceled(new Error('share failed'))).toBe(false);
  });
});
