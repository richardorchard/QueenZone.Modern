import type { KeyValueStorage } from './storage';

export type ContentCacheOptions = {
  storage: KeyValueStorage;
  /** Hard cap on stored detail payloads (LRU eviction). Default 40. */
  maxEntries?: number;
  /** Prefix for entry keys. Default `qz:content:`. */
  keyPrefix?: string;
};

type StoredEnvelope = {
  accessedAt: string;
  /** Monotonic tie-breaker when `accessedAt` collides within the same ms. */
  accessSeq: number;
  cachedAt: string;
  payloadJson: string;
};

const DEFAULT_MAX_ENTRIES = 40;
const DEFAULT_PREFIX = 'qz:content:';

/**
 * Bounded offline store for previously opened archive detail JSON.
 * Evicts least-recently-accessed entries when over {@link ContentCacheOptions.maxEntries}.
 */
export class ContentCache {
  private readonly storage: KeyValueStorage;
  private readonly maxEntries: number;
  private readonly keyPrefix: string;
  private accessSeq = 0;

  constructor(options: ContentCacheOptions) {
    this.storage = options.storage;
    this.maxEntries = options.maxEntries ?? DEFAULT_MAX_ENTRIES;
    this.keyPrefix = options.keyPrefix ?? DEFAULT_PREFIX;
  }

  entryKey(cacheKey: string): string {
    return `${this.keyPrefix}${cacheKey}`;
  }

  async get<T>(cacheKey: string): Promise<T | null> {
    const raw = await this.storage.getItem(this.entryKey(cacheKey));
    if (!raw) {
      return null;
    }

    let envelope: StoredEnvelope;
    try {
      envelope = JSON.parse(raw) as StoredEnvelope;
    } catch {
      await this.storage.removeItem(this.entryKey(cacheKey));
      return null;
    }

    if (typeof envelope.payloadJson !== 'string') {
      await this.storage.removeItem(this.entryKey(cacheKey));
      return null;
    }

    let payload: T;
    try {
      payload = JSON.parse(envelope.payloadJson) as T;
    } catch {
      await this.storage.removeItem(this.entryKey(cacheKey));
      return null;
    }

    const now = new Date().toISOString();
    envelope.accessedAt = now;
    envelope.accessSeq = ++this.accessSeq;
    await this.storage.setItem(this.entryKey(cacheKey), JSON.stringify(envelope));
    return payload;
  }

  async put<T>(cacheKey: string, payload: T): Promise<void> {
    const now = new Date().toISOString();
    const envelope: StoredEnvelope = {
      accessedAt: now,
      accessSeq: ++this.accessSeq,
      cachedAt: now,
      payloadJson: JSON.stringify(payload),
    };
    await this.storage.setItem(this.entryKey(cacheKey), JSON.stringify(envelope));
    await this.evictIfNeeded();
  }

  async clear(): Promise<void> {
    const keys = await this.listEntryKeys();
    if (keys.length > 0) {
      await this.storage.multiRemove(keys);
    }
  }

  /** Test/helper: number of stored detail entries. */
  async size(): Promise<number> {
    return (await this.listEntryKeys()).length;
  }

  private async listEntryKeys(): Promise<string[]> {
    const all = await this.storage.getAllKeys();
    return all.filter((key) => key.startsWith(this.keyPrefix));
  }

  private async evictIfNeeded(): Promise<void> {
    const keys = await this.listEntryKeys();
    if (keys.length <= this.maxEntries) {
      return;
    }

    const scored: { key: string; accessedAt: string; accessSeq: number }[] = [];
    for (const key of keys) {
      const raw = await this.storage.getItem(key);
      if (!raw) {
        continue;
      }
      try {
        const envelope = JSON.parse(raw) as StoredEnvelope;
        scored.push({
          key,
          accessedAt: typeof envelope.accessedAt === 'string' ? envelope.accessedAt : '',
          accessSeq: typeof envelope.accessSeq === 'number' ? envelope.accessSeq : 0,
        });
      } catch {
        scored.push({ key, accessedAt: '', accessSeq: 0 });
      }
    }

    scored.sort((a, b) => {
      const byTime = a.accessedAt.localeCompare(b.accessedAt);
      return byTime !== 0 ? byTime : a.accessSeq - b.accessSeq;
    });
    const removeCount = scored.length - this.maxEntries;
    if (removeCount <= 0) {
      return;
    }
    await this.storage.multiRemove(scored.slice(0, removeCount).map((s) => s.key));
  }
}
