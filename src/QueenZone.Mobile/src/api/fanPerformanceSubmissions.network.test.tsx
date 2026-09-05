import { createFanPerformanceSubmission, reportFanPerformance } from './fanPerformanceSubmissions';
import { ApiError } from './errors';
import { sendJson, sendMultipart } from './client';
import { appendUploadFile } from './uploadFile';
import { reportApiFailure } from '../config/sentry';

jest.mock('./client', () => ({
  sendJson: jest.fn(),
  sendMultipart: jest.fn(),
}));

jest.mock('./uploadFile', () => ({
  appendUploadFile: jest.fn(),
}));

jest.mock('../config/sentry', () => ({
  reportApiFailure: jest.fn(),
}));

const sendJsonMock = sendJson as jest.MockedFunction<typeof sendJson>;
const sendMultipartMock = sendMultipart as jest.MockedFunction<typeof sendMultipart>;
const appendUploadFileMock = appendUploadFile as jest.MockedFunction<typeof appendUploadFile>;

describe('createFanPerformanceSubmission', () => {
  beforeEach(() => {
    sendMultipartMock.mockReset();
    appendUploadFileMock.mockReset();
    appendUploadFileMock.mockResolvedValue(undefined);
  });

  it('sends multipart through the shared upload helper', async () => {
    sendMultipartMock.mockResolvedValue({
      id: '11111111-1111-1111-1111-111111111111',
      status: 'Pending',
      title: 'Cover',
      submittedAt: '2026-09-04T00:15:00.000Z',
    });

    const created = await createFanPerformanceSubmission(
      {
        title: 'Cover',
        coveredSong: 'Liar',
        performedBy: 'Fan',
        description: 'Take 2',
        rightsDeclarationAccepted: true,
        audio: { uri: 'file://cover.mp3', name: 'cover.mp3', type: 'audio/mpeg' },
      },
      'tok',
    );

    expect(appendUploadFileMock).toHaveBeenCalled();
    expect(sendMultipartMock).toHaveBeenCalledWith(
      '/member/fan-performance-submissions',
      expect.any(FormData),
      { accessToken: 'tok', signal: undefined },
    );
    expect(created.id).toBe('11111111-1111-1111-1111-111111111111');
  });

  it('reports a local-file failure and rethrows', async () => {
    const local = ApiError.localFile(new Error('enoent'));
    appendUploadFileMock.mockRejectedValue(local);

    await expect(
      createFanPerformanceSubmission(
        {
          title: 'Cover',
          coveredSong: 'Liar',
          performedBy: 'Fan',
          rightsDeclarationAccepted: true,
          audio: { uri: 'file://missing.mp3', name: 'missing.mp3', type: 'audio/mpeg' },
        },
        'tok',
      ),
    ).rejects.toBe(local);
    expect(reportApiFailure).toHaveBeenCalled();
    expect(sendMultipartMock).not.toHaveBeenCalled();
  });
});

describe('reportFanPerformance', () => {
  beforeEach(() => {
    sendJsonMock.mockReset();
  });

  it('posts the reason and returns alreadyReported', async () => {
    sendJsonMock.mockResolvedValue({ reportId: 'rep-1', alreadyReported: true });
    const created = await reportFanPerformance(187, 'Rights issue', 'tok');
    expect(sendJsonMock).toHaveBeenCalledWith('/me/fan-performances/187/report', {
      accessToken: 'tok',
      signal: undefined,
      body: { reason: 'Rights issue' },
    });
    expect(created).toEqual({ reportId: 'rep-1', alreadyReported: true });
  });

  it('rejects a payload without a report id', async () => {
    sendJsonMock.mockResolvedValue({ reportId: '', alreadyReported: false });
    await expect(reportFanPerformance(187, 'x', 'tok')).rejects.toThrow(/id/);
  });

  it('treats a missing alreadyReported flag as a new report', async () => {
    sendJsonMock.mockResolvedValue({ reportId: 'rep-2' });
    const created = await reportFanPerformance(187, 'Rights issue', 'tok');
    expect(created.alreadyReported).toBe(false);
  });
});
