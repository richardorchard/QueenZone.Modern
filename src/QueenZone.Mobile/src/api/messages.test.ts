import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  messagesApiPath,
  messagesConversationPath,
  messagesRecipientsPath,
  messagesUnreadCountPath,
} from './messagesPaths.ts';

describe('messages API paths', () => {
  it('nests inbox under the signed-in member API', () => {
    assert.equal(messagesApiPath, '/me/messages');
    assert.equal(messagesUnreadCountPath, '/me/messages/unread-count');
    assert.equal(messagesRecipientsPath, '/me/messages/recipients');
    assert.equal(
      messagesConversationPath('aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'),
      '/me/messages/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
    );
  });
});
