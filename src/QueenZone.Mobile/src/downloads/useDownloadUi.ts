import { downloadUiCacheKey, downloadUiCachePrefix } from '../cache/keys';
import { usePrefixVersion, useStoreVersion } from '../cache/useExternalStore';
import { useSession } from '../session/SessionContext';
import { getDownloadUiSnapshot, listDownloadUiSnapshots } from './uiState';
import type { DownloadUiSnapshot } from './types';

export function useDownloadMemberId(): string | null {
  const { profile } = useSession();
  return profile?.memberId ?? null;
}

export function useDownloadUi(performanceId: number | string): DownloadUiSnapshot | null {
  const memberId = useDownloadMemberId();
  const id = String(performanceId);
  const cacheKey = memberId ? downloadUiCacheKey(memberId, id) : '';
  useStoreVersion(cacheKey);
  if (!memberId) {
    return null;
  }
  return getDownloadUiSnapshot(memberId, id);
}

export function useDownloadUiList(): DownloadUiSnapshot[] {
  const memberId = useDownloadMemberId();
  const prefix = memberId ? downloadUiCachePrefix(memberId) : '';
  usePrefixVersion(prefix);
  if (!memberId) {
    return [];
  }
  return listDownloadUiSnapshots(memberId);
}
