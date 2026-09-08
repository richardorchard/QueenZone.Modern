/**
 * Resolve the #1322 Release smoke-embed flag from baked `extra` or a
 * Metro-inlined `EXPO_PUBLIC_SMOKE_EMBED` token. Constants.expoConfig can be
 * empty on the first JS tick of an iOS Release embed; the public env token is
 * available immediately because Metro inlines EXPO_PUBLIC_* at bundle time.
 */
export function resolveSmokeEmbedFlag(
  extra: { smokeEmbed?: boolean | string } = {},
  env: Record<string, string | undefined> = typeof process === 'undefined' ? {} : process.env,
): boolean {
  if (extra.smokeEmbed === true || extra.smokeEmbed === 'true') {
    return true;
  }
  const raw = (env.EXPO_PUBLIC_SMOKE_EMBED ?? '').trim().toLowerCase();
  return raw === '1' || raw === 'true' || raw === 'yes';
}
