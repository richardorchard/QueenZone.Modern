/**
 * Resolve the #1322 Release smoke-embed flag from baked `extra` or a
 * Metro-inlined `EXPO_PUBLIC_SMOKE_EMBED` token. Constants.expoConfig can be
 * empty on the first JS tick of an iOS Release embed; the public env token is
 * available immediately because Metro inlines EXPO_PUBLIC_* at bundle time.
 *
 * The default path must read `process.env.EXPO_PUBLIC_SMOKE_EMBED` as a static
 * member. babel-preset-expo 57 only inlines that shape — a lookup on a passed
 * env bag stays `undefined` on device (#1404 / #1405).
 */
function isTruthySmokeEmbedToken(raw: string | undefined): boolean {
  const token = (raw ?? '').trim().toLowerCase();
  return token === '1' || token === 'true' || token === 'yes';
}

export function resolveSmokeEmbedFlag(
  extra: { smokeEmbed?: boolean | string } = {},
  env?: Record<string, string | undefined>,
): boolean {
  if (extra.smokeEmbed === true || extra.smokeEmbed === 'true') {
    return true;
  }
  if (env) {
    return isTruthySmokeEmbedToken(env.EXPO_PUBLIC_SMOKE_EMBED);
  }
  return isTruthySmokeEmbedToken(process.env.EXPO_PUBLIC_SMOKE_EMBED);
}
