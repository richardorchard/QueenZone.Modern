import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  attachmentFromPickerAsset,
  composerAttachCopy,
  composerCopy,
  composerMode,
  fileNameFromUri,
  forumImagePickerOptions,
  guessForumAttachmentType,
  validateComposer,
} from './composerMeta.ts';

describe('composerMode', () => {
  it('treats a thread id as reply and everything else as a new topic', () => {
    assert.equal(composerMode({ threadId: 1002 }), 'reply');
    assert.equal(composerMode({ categoryId: 1 }), 'newTopic');
    assert.equal(composerMode({}), 'newTopic');
    assert.equal(composerMode(undefined), 'newTopic');
  });
});

describe('validateComposer', () => {
  it('requires a board and 5-200 character title for new topics', () => {
    assert.equal(
      validateComposer({ mode: 'newTopic', title: 'Hey', body: 'Hello fans', categoryId: 1 }),
      'Title must be between 5 and 200 characters.',
    );
    assert.equal(
      validateComposer({ mode: 'newTopic', title: 'Fresh forum news', body: 'Hello fans' }),
      'Choose a board for this topic.',
    );
    assert.equal(
      validateComposer({
        mode: 'newTopic',
        title: 'Fresh forum news',
        body: 'Hello fans',
        categoryId: 1,
      }),
      null,
    );
  });

  it('requires a body for replies and does not require a title', () => {
    assert.equal(
      validateComposer({ mode: 'reply', title: '', body: '   ' }),
      'Write a post before publishing.',
    );
    assert.equal(validateComposer({ mode: 'reply', title: '', body: 'A reply' }), null);
    assert.equal(
      validateComposer({ mode: 'reply', title: '', body: 'A reply', isLocked: true }),
      'This topic is locked.',
    );
  });
});

describe('composerCopy', () => {
  it('labels reply vs new topic actions', () => {
    assert.deepEqual(composerCopy('reply'), { title: 'Reply', action: 'Post reply' });
    assert.deepEqual(composerCopy('newTopic'), { title: 'New topic', action: 'Post topic' });
  });
});

describe('forumImagePickerOptions', () => {
  it('keeps the original image without cropping', () => {
    assert.deepEqual(forumImagePickerOptions.mediaTypes, ['images']);
    assert.equal(forumImagePickerOptions.quality, 1);
    assert.equal(forumImagePickerOptions.allowsEditing, false);
  });
});

describe('attachmentFromPickerAsset', () => {
  it('maps photo and document picker fields onto one upload part', () => {
    assert.deepEqual(
      attachmentFromPickerAsset({
        uri: 'file:///photos/crowd.jpg',
        fileName: 'crowd.jpg',
        mimeType: 'image/jpeg',
      }),
      { uri: 'file:///photos/crowd.jpg', name: 'crowd.jpg', type: 'image/jpeg' },
    );
    assert.deepEqual(
      attachmentFromPickerAsset({
        uri: 'file:///docs/notes.pdf',
        name: 'notes.pdf',
        mimeType: 'application/pdf',
      }),
      { uri: 'file:///docs/notes.pdf', name: 'notes.pdf', type: 'application/pdf' },
    );
  });

  it('guesses type from the name when the picker only has octet-stream', () => {
    const guessed: [string, string][] = [
      ['scan.PDF', 'application/pdf'],
      ['crowd.jpg', 'image/jpeg'],
      ['crowd.jpeg', 'image/jpeg'],
      ['crowd.png', 'image/png'],
      ['crowd.gif', 'image/gif'],
      ['crowd.webp', 'image/webp'],
      ['pack.zip', 'application/zip'],
      ['solo.mp3', 'audio/mpeg'],
      ['solo.flac', 'audio/flac'],
      ['notes.txt', 'text/plain'],
      ['notes.doc', 'application/msword'],
      ['notes.docx', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'],
      ['sheet.xls', 'application/vnd.ms-excel'],
      ['sheet.xlsx', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'],
      ['deck.ppt', 'application/vnd.ms-powerpoint'],
      ['deck.pptx', 'application/vnd.openxmlformats-officedocument.presentationml.presentation'],
      ['unknown.bin', 'application/octet-stream'],
    ];
    for (const [name, type] of guessed) {
      assert.equal(guessForumAttachmentType(name), type);
    }
    assert.equal(fileNameFromUri('file:///tmp/setlist%20notes.txt'), 'setlist notes.txt');
    assert.equal(fileNameFromUri('file:///tmp/%E0%A4%A'), '%E0%A4%A');
    assert.deepEqual(
      attachmentFromPickerAsset({
        uri: 'content://downloads/setlist.txt',
        name: 'setlist.txt',
        mimeType: 'application/octet-stream',
      }),
      { uri: 'content://downloads/setlist.txt', name: 'setlist.txt', type: 'text/plain' },
    );
    assert.deepEqual(
      attachmentFromPickerAsset({
        uri: 'file:///tmp/photo.jpg',
        fileName: 'photo.jpg',
        mimeType: 'image/jpg',
      }),
      { uri: 'file:///tmp/photo.jpg', name: 'photo.jpg', type: 'image/jpeg' },
    );
  });

  it('does not invent a client-side type rejection', () => {
    assert.deepEqual(
      attachmentFromPickerAsset({
        uri: 'file:///photos/crowd.heic',
        fileName: 'crowd.heic',
        mimeType: 'image/heic',
      }),
      { uri: 'file:///photos/crowd.heic', name: 'crowd.heic', type: 'image/heic' },
    );
    assert.deepEqual(attachmentFromPickerAsset({ uri: '' }), { error: composerAttachCopy.missingFile });
  });
});
