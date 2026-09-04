import { fetchJson, sendJson } from './client';
import {
  defaultNotificationPreferences,
  fetchNotificationPreferences,
  notificationPreferencesApiPath,
  parseNotificationPreferences,
  patchNotificationPreferences,
} from './notificationPreferences';

jest.mock('./client', () => ({
  fetchJson: jest.fn(),
  sendJson: jest.fn(),
}));

const fetchJsonMock = fetchJson as jest.MockedFunction<typeof fetchJson>;
const sendJsonMock = sendJson as jest.MockedFunction<typeof sendJson>;

const sample = { forumReply: true, privateMessage: true, news: true };

describe('parseNotificationPreferences', () => {
  it('reads the three category toggles', () => {
    expect(parseNotificationPreferences(sample)).toEqual(defaultNotificationPreferences);
    expect(parseNotificationPreferences({ forumReply: false, privateMessage: false, news: true })).toEqual({
      forumReply: false,
      privateMessage: false,
      news: true,
    });
  });

  it('rejects empty, non-object, and incomplete payloads', () => {
    expect(() => parseNotificationPreferences(null)).toThrow(/empty/);
    expect(() => parseNotificationPreferences([])).toThrow(/empty/);
    expect(() => parseNotificationPreferences({ forumReply: true, privateMessage: true })).toThrow(/missing category/);
    expect(() =>
      parseNotificationPreferences({ forumReply: 'yes', privateMessage: true, news: false }),
    ).toThrow(/missing category/);
  });

  it('ignores extra additive v1 fields', () => {
    expect(parseNotificationPreferences({ ...sample, digest: true })).toEqual(sample);
  });
});

describe('notification preference client', () => {
  beforeEach(() => {
    fetchJsonMock.mockReset();
    sendJsonMock.mockReset();
  });

  it('GETs parsed preferences', async () => {
    fetchJsonMock.mockResolvedValue(sample);
    await expect(fetchNotificationPreferences('tok')).resolves.toEqual(sample);
    expect(fetchJsonMock).toHaveBeenCalledWith(notificationPreferencesApiPath, { accessToken: 'tok' });
  });

  it('PATCHes a partial update and parses the saved body', async () => {
    sendJsonMock.mockResolvedValue({ ...sample, news: true });
    await expect(patchNotificationPreferences('tok', { news: true })).resolves.toEqual({
      forumReply: true,
      privateMessage: true,
      news: true,
    });
    expect(sendJsonMock).toHaveBeenCalledWith(notificationPreferencesApiPath, {
      method: 'PATCH',
      accessToken: 'tok',
      body: { news: true },
    });
  });
});
