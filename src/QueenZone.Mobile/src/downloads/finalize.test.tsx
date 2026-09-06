import {
  adoptProgressTotal,
  canPromotePart,
  classifyPromoteError,
  isTinyCompleteDownload,
  messageForFinalizeFailure,
  MIN_PLAUSIBLE_AUDIO_BYTES,
  resolveDownloadPartUri,
} from './finalize';
import {
  DOWNLOAD_EMPTY_PART_MESSAGE,
  DOWNLOAD_FAILED_MESSAGE,
  DOWNLOAD_INCOMPLETE_MESSAGE,
  DOWNLOAD_PART_MISSING_MESSAGE,
  DOWNLOAD_TOO_SMALL_MESSAGE,
} from './messages';

describe('resolveDownloadPartUri', () => {
  it('prefers the returned task File URI over the assumed dest', () => {
    expect(
      resolveDownloadPartUri('file:///documents/fan-performances/178.part', 'file:///cache/178-task'),
    ).toBe('file:///cache/178-task');
    expect(resolveDownloadPartUri('file:///documents/fan-performances/178.part', '  ')).toBe(
      'file:///documents/fan-performances/178.part',
    );
    expect(resolveDownloadPartUri('file:///documents/fan-performances/178.part', null)).toBe(
      'file:///documents/fan-performances/178.part',
    );
  });
});

describe('adoptProgressTotal', () => {
  it('keeps the probe Content-Length when progress total is a tiny fake-complete', () => {
    expect(adoptProgressTotal(5_000_000, 200)).toBe(5_000_000);
    expect(adoptProgressTotal(null, 200)).toBeNull();
    expect(adoptProgressTotal(4, 4)).toBe(4);
    expect(adoptProgressTotal(1_024_000, 1_024_000)).toBe(1_024_000);
    expect(adoptProgressTotal(null, 0)).toBeNull();
  });
});

describe('isTinyCompleteDownload', () => {
  it('rejects a 100% progress total that is smaller than a plausible audio file', () => {
    expect(isTinyCompleteDownload({ size: 200, probeExpected: null, progressTotal: 200 })).toBe(true);
    expect(
      isTinyCompleteDownload({
        size: 200,
        probeExpected: 5_000_000,
        progressTotal: 200,
      }),
    ).toBe(true);
    expect(isTinyCompleteDownload({ size: 4, probeExpected: 4, progressTotal: 4 })).toBe(false);
    expect(
      isTinyCompleteDownload({
        size: MIN_PLAUSIBLE_AUDIO_BYTES + 1,
        probeExpected: null,
        progressTotal: null,
      }),
    ).toBe(false);
    expect(isTinyCompleteDownload({ size: 0, probeExpected: null, progressTotal: 0 })).toBe(false);
  });
});

describe('canPromotePart', () => {
  it('never promotes a missing or empty .part', () => {
    expect(canPromotePart(false, 0)).toBe(false);
    expect(canPromotePart(true, 0)).toBe(false);
    expect(canPromotePart(true, 2048)).toBe(true);
  });
});

describe('classifyPromoteError', () => {
  it('maps NoSuchFile / missing-path errors', () => {
    const missing = new Error('NoSuchFileException: .../files/fan-performances/178.part');
    missing.name = 'NoSuchFileException';
    expect(classifyPromoteError(missing)).toBe('no-such-file');
    expect(classifyPromoteError(new Error('ENOENT: no such file'))).toBe('no-such-file');
    expect(classifyPromoteError(new Error('disk full'))).toBe('unknown');
    expect(classifyPromoteError('boom')).toBe('unknown');
  });
});

describe('messageForFinalizeFailure', () => {
  it('uses a clearer string than the generic download failure', () => {
    expect(messageForFinalizeFailure('missing-part')).toBe(DOWNLOAD_PART_MISSING_MESSAGE);
    expect(messageForFinalizeFailure('no-such-file')).toBe(DOWNLOAD_PART_MISSING_MESSAGE);
    expect(messageForFinalizeFailure('empty-part')).toBe(DOWNLOAD_EMPTY_PART_MESSAGE);
    expect(messageForFinalizeFailure('tiny-complete')).toBe(DOWNLOAD_TOO_SMALL_MESSAGE);
    expect(messageForFinalizeFailure('incomplete')).toBe(DOWNLOAD_INCOMPLETE_MESSAGE);
    expect(messageForFinalizeFailure('unknown')).toBe(DOWNLOAD_FAILED_MESSAGE);
    expect(DOWNLOAD_PART_MISSING_MESSAGE).not.toBe(DOWNLOAD_FAILED_MESSAGE);
  });
});
