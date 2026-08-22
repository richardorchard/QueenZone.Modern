export type BuildStampMetadata = {
  version: string;
  buildNumber?: string;
  buildTimestampUtc?: string;
  buildRevision?: string;
};

export function formatBuildStamp(
  metadata: BuildStampMetadata,
  locales?: Intl.LocalesArgument,
): string | null {
  if (!metadata.buildTimestampUtc) {
    return null;
  }

  const builtAt = new Date(metadata.buildTimestampUtc);
  if (Number.isNaN(builtAt.getTime())) {
    return null;
  }

  const localTime = new Intl.DateTimeFormat(locales, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(builtAt);
  const version = metadata.buildNumber
    ? `${metadata.version} (${metadata.buildNumber})`
    : metadata.version;
  const revision = metadata.buildRevision ? ` · ${metadata.buildRevision}` : '';
  return `Build ${version} · ${localTime}${revision}`;
}
