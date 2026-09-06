import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  adoptProgressTotal,
  canPromotePart,
  classifyPromoteError,
  isTinyCompleteDownload,
  messageForFinalizeFailure,
  MIN_PLAUSIBLE_AUDIO_BYTES,
  resolveDownloadPartUri,
} from './finalize.ts';
import {
  DOWNLOAD_EMPTY_PART_MESSAGE,
  DOWNLOAD_FAILED_MESSAGE,
  DOWNLOAD_INCOMPLETE_MESSAGE,
  DOWNLOAD_PART_MISSING_MESSAGE,
  DOWNLOAD_TOO_SMALL_MESSAGE,
} from './messages.ts';

describe('resolveDownloadPartUri', () => {
  it('prefers the returned task File URI over the assumed dest', () => {
    assert.equal(
      resolveDownloadPartUri('file:///documents/fan-performances/178.part', 'file:///cache/178-task'),
      'file:///cache/178-task',
    );
    assert.equal(
      resolveDownloadPartUri('file:///documents/fan-performances/178.part', '  '),
      'file:///documents/fan-performances/178.part',
    );
    assert.equal(
      resolveDownloadPartUri('file:///documents/fan-performances/178.part', null),
      'file:///documents/fan-performances/178.part',
    );
  });
});

describe('adoptProgressTotal', () => {
  it('keeps the probe Content-Length when progress total is a tiny fake-complete', () => {
    assert.equal(adoptProgressTotal(5_000_000, 200), 5_000_000);
    assert.equal(adoptProgressTotal(null, 200), null);
    assert.equal(adoptProgressTotal(4, 4), 4);
    assert.equal(adoptProgressTotal(1_024_000, 1_024_000), 1_024_000);
    assert.equal(adoptProgressTotal(null, 0), null);
  });
});

describe('isTinyCompleteDownload', () => {
  it('rejects a 100% progress total that is smaller than a plausible audio file', () => {
    assert.equal(
      isTinyCompleteDownload({ size: 200, probeExpected: null, progressTotal: 200 }),
      true,
    );
    assert.equal(
      isTinyCompleteDownload({
        size: 200,
        probeExpected: 5_000_000,
        progressTotal: 200,
      }),
      true,
    );
    assert.equal(
      isTinyCompleteDownload({ size: 4, probeExpected: 4, progressTotal: 4 }),
      false,
    );
    assert.equal(
      isTinyCompleteDownload({
        size: MIN_PLAUSIBLE_AUDIO_BYTES + 1,
        probeExpected: null,
        progressTotal: null,
      }),
      false,
    );
    assert.equal(
      isTinyCompleteDownload({ size: 0, probeExpected: null, progressTotal: 0 }),
      false,
    );
  });
});

describe('canPromotePart', () => {
  it('never promotes a missing or empty .part', () => {
    assert.equal(canPromotePart(false, 0), false);
    assert.equal(canPromotePart(true, 0), false);
    assert.equal(canPromotePart(true, 2048), true);
  });
});

describe('classifyPromoteError', () => {
  it('maps NoSuchFile / missing-path errors', () => {
    const missing = new Error(
      "NoSuchFileException: .../files/fan-performances/178.part",
    );
    missing.name = 'NoSuchFileException';
    assert.equal(classifyPromoteError(missing), 'no-such-file');
    assert.equal(classifyPromoteError(new Error('ENOENT: no such file')), 'no-such-file');
    assert.equal(classifyPromoteError(new Error('disk full')), 'unknown');
    assert.equal(classifyPromoteError('boom'), 'unknown');
  });
});

describe('messageForFinalizeFailure', () => {
  it('uses a clearer string than the generic download failure', () => {
    assert.equal(messageForFinalizeFailure('missing-part'), DOWNLOAD_PART_MISSING_MESSAGE);
    assert.equal(messageForFinalizeFailure('no-such-file'), DOWNLOAD_PART_MISSING_MESSAGE);
    assert.equal(messageForFinalizeFailure('empty-part'), DOWNLOAD_EMPTY_PART_MESSAGE);
    assert.equal(messageForFinalizeFailure('tiny-complete'), DOWNLOAD_TOO_SMALL_MESSAGE);
    assert.equal(messageForFinalizeFailure('incomplete'), DOWNLOAD_INCOMPLETE_MESSAGE);
    assert.equal(messageForFinalizeFailure('unknown'), DOWNLOAD_FAILED_MESSAGE);
    assert.notEqual(DOWNLOAD_PART_MISSING_MESSAGE, DOWNLOAD_FAILED_MESSAGE);
  });
});
