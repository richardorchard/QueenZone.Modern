export const newsShareSlotKey = 'queenzone.newsShare.v1';

export type NewsShareKeyValue = {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
};

export type NewsSuggestDraft = {
  url: string;
  title: string;
  notes: string;
  /** 'share' pre-fills from intake. 'inApp' starts empty. Does not change submit. */
  origin: 'share' | 'inApp';
};

/**
 * Disk shape. `submitting` is not persisted. A crash mid-POST comes back as `form`
 * so the member taps Submit again. No silent offline queue.
 */
export type PersistedNewsShare =
  | { v: 1; kind: 'choose'; candidates: [string, string, ...string[]] }
  | { v: 1; kind: 'form'; draft: NewsSuggestDraft };

export type NewsShareStore = {
  read(): Promise<PersistedNewsShare | null>;
  write(value: PersistedNewsShare): Promise<void>;
  clear(): Promise<void>;
};

export function createNewsShareStore(storage: NewsShareKeyValue): NewsShareStore {
  return {
    async read() {
      const raw = await storage.getItem(newsShareSlotKey);
      if (raw == null) {
        return null;
      }

      const parsed = parsePersisted(raw);
      if (parsed) {
        return parsed;
      }

      await storage.removeItem(newsShareSlotKey);
      return null;
    },
    async write(value) {
      await storage.setItem(newsShareSlotKey, JSON.stringify(value));
    },
    async clear() {
      await storage.removeItem(newsShareSlotKey);
    },
  };
}

function parsePersisted(raw: string): PersistedNewsShare | null {
  let value: unknown;
  try {
    value = JSON.parse(raw);
  } catch {
    return null;
  }

  if (!value || typeof value !== 'object') {
    return null;
  }

  const record = value as Record<string, unknown>;
  if (record.v !== 1) {
    return null;
  }

  if (record.kind === 'choose') {
    const candidates = record.candidates;
    if (!Array.isArray(candidates) || candidates.length < 2 || !candidates.every((item) => typeof item === 'string')) {
      return null;
    }
    return {
      v: 1,
      kind: 'choose',
      candidates: [candidates[0], candidates[1], ...candidates.slice(2)] as [string, string, ...string[]],
    };
  }

  if (record.kind === 'form') {
    const draft = parseDraft(record.draft);
    if (!draft) {
      return null;
    }
    return { v: 1, kind: 'form', draft };
  }

  return null;
}

function parseDraft(value: unknown): NewsSuggestDraft | null {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const draft = value as Record<string, unknown>;
  if (typeof draft.url !== 'string' || typeof draft.title !== 'string' || typeof draft.notes !== 'string') {
    return null;
  }

  if (draft.origin !== 'share' && draft.origin !== 'inApp') {
    return null;
  }

  return {
    url: draft.url,
    title: draft.title,
    notes: draft.notes,
    origin: draft.origin,
  };
}
