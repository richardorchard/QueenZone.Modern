import { ApiError } from '../../api/errors';
import type { NewsSuggestionCreated, NewsSuggestionWrite } from '../../api/newsSuggestions';
import { createMemoryStorage } from '../../cache/storage';
import { createNewsShareStore } from './draftStore';
import { createNewsShareController } from './session';

const httpsUrl = 'https://www.bbc.co.uk/news/example';
const created: NewsSuggestionCreated = {
  id: '11111111-1111-1111-1111-111111111111',
  status: 'Pending',
  url: httpsUrl,
  title: 'Queen announce dates',
  submittedAt: '2026-08-26T10:00:00Z',
};

function controller(submit?: (input: NewsSuggestionWrite, token: string) => Promise<NewsSuggestionCreated>) {
  const store = createNewsShareStore(createMemoryStorage());
  const calls: NewsSuggestionWrite[] = [];
  const session = createNewsShareController(store, async (input, token) => {
    calls.push(input);
    if (submit) {
      return submit(input, token);
    }
    return created;
  });
  return { session, store, calls };
}

describe('news share session', () => {
  it('keeps a captured draft after remount', async () => {
    const storage = createMemoryStorage();
    const store = createNewsShareStore(storage);
    const first = createNewsShareController(store, async () => created);
    await first.capture({ webUrl: httpsUrl, hasFiles: false });
    const form = first.view();
    expect(form.kind).toBe('form');
    if (form.kind === 'form') {
      form.patch({ title: 'Kept after kill' });
    }
    await first.flush();

    const remount = createNewsShareController(createNewsShareStore(storage), async () => created);
    await remount.hydrate();
    const restored = remount.view();
    expect(restored.kind).toBe('form');
    if (restored.kind !== 'form') {
      return;
    }
    expect(restored.draft.url).toBe(httpsUrl);
    expect(restored.draft.title).toBe('Kept after kill');
  });

  it('keeps edits when the same normalized URL is redelivered', async () => {
    const { session } = controller();
    await session.capture({ webUrl: httpsUrl, hasFiles: false });
    const form = session.view();
    expect(form.kind).toBe('form');
    if (form.kind !== 'form') {
      return;
    }
    form.patch({ title: 'Member headline', notes: 'Worth a look' });
    await session.capture({ webUrl: `${httpsUrl}/`, text: 'Queen announce dates', hasFiles: false });
    const again = session.view();
    expect(again.kind).toBe('form');
    if (again.kind !== 'form') {
      return;
    }
    expect(again.draft.title).toBe('Member headline');
    expect(again.draft.notes).toBe('Worth a look');
  });

  it('replaces the draft when a new URL arrives', async () => {
    const { session } = controller();
    await session.capture({ webUrl: httpsUrl, hasFiles: false });
    const form = session.view();
    if (form.kind === 'form') {
      form.patch({ title: 'Old' });
    }
    await session.capture({ webUrl: 'https://www.rollingstone.com/music/queen', hasFiles: false });
    const next = session.view();
    expect(next.kind).toBe('form');
    if (next.kind !== 'form') {
      return;
    }
    expect(next.draft.url).toBe('https://www.rollingstone.com/music/queen');
    expect(next.draft.title).toBe('');
  });

  it('no-ops a second submit while inFlight', async () => {
    let resolveSubmit!: (value: NewsSuggestionCreated) => void;
    const hung = new Promise<NewsSuggestionCreated>((resolve) => {
      resolveSubmit = resolve;
    });
    const { session, calls } = controller(() => hung);
    await session.capture({ webUrl: httpsUrl, hasFiles: false });
    const first = session.view();
    expect(first.kind).toBe('form');
    if (first.kind !== 'form') {
      return;
    }

    const pending = first.submit('tok');
    expect(session.view().kind).toBe('submitting');
    await first.submit('tok');
    resolveSubmit(created);
    await pending;
    expect(calls).toHaveLength(1);
    expect(session.view().kind).toBe('submitted');
  });

  it('flushes the latest patch before a sign-in hop', async () => {
    const { session, store } = controller();
    await session.capture({ webUrl: httpsUrl, hasFiles: false });
    const form = session.view();
    expect(form.kind).toBe('form');
    if (form.kind !== 'form') {
      return;
    }
    form.patch({ title: 'Flush me' });
    await session.flush();
    const persisted = await store.read();
    expect(persisted?.kind).toBe('form');
    if (persisted?.kind === 'form') {
      expect(persisted.draft.title).toBe('Flush me');
    }
  });

  it('maps submit errors for 400, 409, 429, 401, and network', async () => {
    const cases: Array<{ error: unknown; code: string }> = [
      { error: new ApiError(400, 'Bad Request'), code: 'invalid' },
      { error: new ApiError(409, 'Conflict'), code: 'duplicate' },
      { error: new ApiError(429, 'Too Many Requests'), code: 'quota' },
      { error: new ApiError(401, 'Unauthorized'), code: 'unauthorized' },
      { error: new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.'), code: 'network' },
    ];

    for (const example of cases) {
      const { session, store } = controller(async () => {
        throw example.error;
      });
      await session.capture({ webUrl: httpsUrl, hasFiles: false });
      const form = session.view();
      expect(form.kind).toBe('form');
      if (form.kind !== 'form') {
        return;
      }
      await form.submit('tok');
      const failed = session.view();
      expect(failed.kind).toBe('failed');
      if (failed.kind !== 'failed') {
        return;
      }
      expect(failed.error.code).toBe(example.code);
      expect(await store.read()).not.toBeNull();
    }
  });

  it('clears the slot on cancel and on 201', async () => {
    const { session, store } = controller();
    await session.capture({ webUrl: httpsUrl, hasFiles: false });
    const form = session.view();
    if (form.kind === 'form') {
      form.cancel();
    }
    await session.flush();
    expect(await store.read()).toBeNull();

    await session.capture({ webUrl: httpsUrl, hasFiles: false });
    const again = session.view();
    if (again.kind === 'form') {
      await again.submit('tok');
    }
    expect(session.view().kind).toBe('submitted');
    expect(await store.read()).toBeNull();
  });

  it('hydrate after a rejected share drops the ephemeral reject', async () => {
    const { session } = controller();
    await session.capture({ hasFiles: true });
    expect(session.view().kind).toBe('rejected');
    await session.hydrate();
    expect(session.view().kind).toBe('idle');
  });

  it('does not write a draft when the member picks http from choose', async () => {
    const { session, store } = controller();
    await session.capture({
      text: 'http://example.com/old https://example.com/new',
      hasFiles: false,
    });
    const choose = session.view();
    expect(choose.kind).toBe('choose');
    if (choose.kind !== 'choose') {
      return;
    }
    choose.choose('http://example.com/old');
    const rejected = session.view();
    expect(rejected.kind).toBe('rejected');
    if (rejected.kind !== 'rejected') {
      return;
    }
    expect(rejected.reason).toBe('notHttps');
    const persisted = await store.read();
    expect(persisted?.kind).toBe('choose');
  });
});
