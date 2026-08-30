import type { KeyValueStorage } from './storage';

export type ContentCacheOptions = {
  storage: KeyValueStorage;
  /** Hard cap on stored detail payloads (LRU eviction). Default 80. */
  maxEntries?: number;
  /** Prefix for entry keys. Default `qz:content:`. */
  keyPrefix?: string;
  /** Envelope schema. Mismatched or missing versions self-delete. */
  schemaVersion?: number;
};

export type CacheRecord<T> = {
  payload: T;
  cachedAt: string;
};

type StoredEnvelope = {
  schemaVersion: number;
  accessedAt: string;
  /** Monotonic tie-breaker when `accessedAt` collides within the same ms. */
  accessSeq: number;
  cachedAt: string;
  payloadJson: string;
};

/** First versioned envelope. Unversioned rows from the archive-only cache self-delete. */
export const CONTENT_CACHE_SCHEMA_VERSION = 1;

/**
 * Device LRU cap after #764 sizing: archive details plus forum topic/page
 * snapshots and member-scoped conversations. See hosting-scale-and-cache.md.
 */
export const CONTENT_CACHE_MAX_ENTRIES = 80;

const DEFAULT_PREFIX = 'qz:content:';

function isStoredEnvelope(value: unknown): value is StoredEnvelope {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const envelope = value as StoredEnvelope;
  return (
    typeof envelope.schemaVersion === 'number' &&
    typeof envelope.accessedAt === 'string' &&
    typeof envelope.accessSeq === 'number' &&
    typeof envelope.cachedAt === 'string' &&
    typeof envelope.payloadJson === 'string'
  );
}

/**
 * Bounded offline store for previously opened detail JSON.
 * Evicts least-recently-accessed entries when over {@link ContentCacheOptions.maxEntries}.
 * Conversation bodies live here (AsyncStorage), never in SecureStore.
 */
export class ContentCache {
  private readonly storage: KeyValueStorage;
  private readonly maxEntries: number;
  private readonly keyPrefix: string;
  private readonly schemaVersion: number;
  private accessSeq = 0;

  constructor(options: ContentCacheOptions) {
    this.storage = options.storage;
    this.maxEntries = options.maxEntries ?? CONTENT_CACHE_MAX_ENTRIES;
    this.keyPrefix = options.keyPrefix ?? DEFAULT_PREFIX;
    this.schemaVersion = options.schemaVersion ?? CONTENT_CACHE_SCHEMA_VERSION;
  }

  entryKey(cacheKey: string): string {
    return `${this.keyPrefix}${cacheKey}`;
  }

  async get<T>(cacheKey: string): Promise<T | null> {
    const record = await this.read<T>(cacheKey);
    return record ? record.payload : null;
  }

  async read<T>(cacheKey: string): Promise<CacheRecord<T> | null> {
    const storageKey = this.entryKey(cacheKey);
    const raw = await this.storage.getItem(storageKey);
    if (!raw) {
      return null;
    }

    let parsed: unknown;
    try {
      parsed = JSON.parse(raw);
    } catch {
      await this.storage.removeItem(storageKey);
      return null;
    }

    if (!isStoredEnvelope(parsed) || parsed.schemaVersion !== this.schemaVersion) {
      await this.storage.removeItem(storageKey);
      return null;
    }

    let payload: T;
    try {
      payload = JSON.parse(parsed.payloadJson) as T;
    } catch {
      await this.storage.removeItem(storageKey);
      return null;
    }

    const now = new Date().toISOString();
    parsed.accessedAt = now;
    parsed.accessSeq = ++this.accessSeq;
    await this.storage.setItem(storageKey, JSON.stringify(parsed));
    return { payload, cachedAt: parsed.cachedAt };
  }

  async put<T>(cacheKey: string, payload: T): Promise<string> {
    const now = new Date().toISOString();
    const envelope: StoredEnvelope = {
      schemaVersion: this.schemaVersion,
      accessedAt: now,
      accessSeq: ++this.accessSeq,
      cachedAt: now,
      payloadJson: JSON.stringify(payload),
    };
    await this.storage.setItem(this.entryKey(cacheKey), JSON.stringify(envelope));
    await this.evictIfNeeded();
    return now;
  }

  async remove(cacheKey: string): Promise<void> {
    await this.storage.removeItem(this.entryKey(cacheKey));
  }

  /**
   * Delete stored entries whose logical cache key starts with `cacheKeyPrefix`
   * (the prefix after {@link ContentCacheOptions.keyPrefix}).
   */
  async purgePrefix(cacheKeyPrefix: string): Promise<void> {
    const storagePrefix = this.entryKey(cacheKeyPrefix);
    const keys = (await this.listEntryKeys()).filter((key) => key.startsWith(storagePrefix));
    if (keys.length > 0) {
      await this.storage.multiRemove(keys);
    }
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

  async listCacheKeys(): Promise<string[]> {
    const keys = await this.listEntryKeys();
    return keys.map((key) => key.slice(this.keyPrefix.length));
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
