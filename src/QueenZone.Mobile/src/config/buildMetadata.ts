export type BuildStampMetadata = {
  version: string;
  buildNumber?: string;
  buildTimestampUtc?: string;
  buildRevision?: string;
};

const utcShortMonths = [
  'Jan',
  'Feb',
  'Mar',
  'Apr',
  'May',
  'Jun',
  'Jul',
  'Aug',
  'Sep',
  'Oct',
  'Nov',
  'Dec',
] as const;

/** Calendar date from the baked UTC timestamp. No second clock, no time-of-day. */
export function formatHomeFooterDate(buildTimestampUtc?: string): string | null {
  if (!buildTimestampUtc) {
    return null;
  }

  const builtAt = new Date(buildTimestampUtc);
  if (Number.isNaN(builtAt.getTime())) {
    return null;
  }

  return `${builtAt.getUTCDate()} ${utcShortMonths[builtAt.getUTCMonth()]} ${builtAt.getUTCFullYear()}`;
}

/** Home footer: store Version, plus the publish date when the timestamp is baked. */
export function formatHomeFooter(metadata: Pick<BuildStampMetadata, 'version' | 'buildTimestampUtc'>): string {
  const date = formatHomeFooterDate(metadata.buildTimestampUtc);
  return date ? `${metadata.version} · ${date}` : metadata.version;
}

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
