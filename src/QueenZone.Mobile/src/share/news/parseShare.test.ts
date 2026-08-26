import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { leftoverAfterUrls, parseShare } from './parseShare.ts';

describe('parseShare', () => {
  it('accepts a dedicated url field', () => {
    const intake = parseShare({ webUrl: 'https://www.bbc.co.uk/news/example', hasFiles: false });
    assert.deepEqual(intake, {
      kind: 'accepted',
      url: 'https://www.bbc.co.uk/news/example',
      leftoverText: '',
    });
  });

  it('accepts a https URL embedded in text and keeps leftover title ≤300', () => {
    const intake = parseShare({
      text: 'Queen announce dates https://www.bbc.co.uk/news/example tonight',
      hasFiles: false,
    });
    assert.equal(intake.kind, 'accepted');
    if (intake.kind !== 'accepted') {
      return;
    }
    assert.equal(intake.url, 'https://www.bbc.co.uk/news/example');
    assert.equal(intake.leftoverText, 'Queen announce dates tonight');
  });

  it('treats a duplicated same URL as one accepted link', () => {
    const intake = parseShare({
      webUrl: 'https://www.bbc.co.uk/news/example',
      text: 'See https://www.bbc.co.uk/news/example',
      hasFiles: false,
    });
    assert.equal(intake.kind, 'accepted');
  });

  it('asks the member to choose when two https URLs are present', () => {
    const intake = parseShare({
      text: 'https://www.bbc.co.uk/one https://www.bbc.co.uk/two',
      hasFiles: false,
    });
    assert.deepEqual(intake, {
      kind: 'choose',
      candidates: ['https://www.bbc.co.uk/one', 'https://www.bbc.co.uk/two'],
    });
  });

  it('rejects http-only shares without persisting an upgrade path', () => {
    const intake = parseShare({ text: 'http://www.bbc.co.uk/news/example', hasFiles: false });
    assert.equal(intake.kind, 'rejected');
    if (intake.kind !== 'rejected') {
      return;
    }
    assert.equal(intake.reason, 'notHttps');
  });

  it('rejects a file share even when the caption contains a URL', () => {
    const intake = parseShare({
      text: 'Photo of https://www.bbc.co.uk/news/example',
      hasFiles: true,
    });
    assert.equal(intake.kind, 'rejected');
    if (intake.kind !== 'rejected') {
      return;
    }
    assert.equal(intake.reason, 'file');
  });

  it('rejects javascript: payloads', () => {
    const intake = parseShare({ text: 'javascript:alert(1)', hasFiles: false });
    assert.equal(intake.kind, 'rejected');
    if (intake.kind !== 'rejected') {
      return;
    }
    assert.equal(intake.reason, 'unsupportedScheme');
  });

  it('rejects a custom scheme', () => {
    const intake = parseShare({ text: 'queenzone://story/9', hasFiles: false });
    assert.equal(intake.kind, 'rejected');
    if (intake.kind !== 'rejected') {
      return;
    }
    assert.equal(intake.reason, 'unsupportedScheme');
  });

  it('puts mixed http and https into choose instead of auto-picking https', () => {
    const intake = parseShare({
      text: 'http://example.com/old https://example.com/new',
      hasFiles: false,
    });
    assert.equal(intake.kind, 'choose');
    if (intake.kind !== 'choose') {
      return;
    }
    assert.deepEqual(intake.candidates, ['http://example.com/old', 'https://example.com/new']);
  });

  it('discards leftover title text longer than 300 characters', () => {
    const leftover = leftoverAfterUrls(`${'Q'.repeat(301)} https://example.com/story`, [
      { scheme: 'https', href: 'https://example.com/story' },
    ]);
    assert.equal(leftover, '');

    const short = leftoverAfterUrls('Queen announce dates https://example.com/story', [
      { scheme: 'https', href: 'https://example.com/story' },
    ]);
    assert.equal(short, 'Queen announce dates');
    assert.ok(short.length <= 300);
  });

  it('rejects shares with no URL', () => {
    const intake = parseShare({ text: 'Just a thought about the gig', hasFiles: false });
    assert.equal(intake.kind, 'rejected');
    if (intake.kind !== 'rejected') {
      return;
    }
    assert.equal(intake.reason, 'noUrl');
  });
});
