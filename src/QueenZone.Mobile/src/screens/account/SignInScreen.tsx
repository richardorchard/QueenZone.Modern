import { useCallback, useEffect, useState } from 'react';
import { ScrollView, Text, View } from 'react-native';
import { getAppConfig } from '../../config/appConfig';
import { authProvidersUrl, fallbackAuthProviders, parseAuthProviders, type AuthProvider } from '../../api/auth';
import { useSession } from '../../session/SessionContext';
import { space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { CrestSeal } from '../../ui/CrestSeal';

export function SignInScreen() {
  const { c } = useTheme();
  const { signIn } = useSession();
  const [providers, setProviders] = useState<AuthProvider[]>(fallbackAuthProviders);
  const [busyProvider, setBusyProvider] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const response = await fetch(authProvidersUrl(getAppConfig().apiBaseUrl), {
          headers: { Accept: 'application/json' },
        });
        const payload: unknown = await response.json().catch(() => null);
        if (!cancelled && response.ok) {
          setProviders(parseAuthProviders(payload));
        }
      } catch {
        if (!cancelled) {
          setProviders(fallbackAuthProviders);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const onProvider = useCallback(
    async (provider: AuthProvider) => {
      if (busyProvider) {
        return;
      }

      setError(null);
      setBusyProvider(provider.id);
      try {
        await signIn(provider.id);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Could not sign in.');
      } finally {
        setBusyProvider(null);
      }
    },
    [busyProvider, signIn],
  );

  return (
    <ScrollView
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      contentContainerStyle={{
        paddingHorizontal: space.xl,
        paddingTop: space.section,
        paddingBottom: space.section,
        gap: space.lg,
      }}
    >
      <View style={{ alignItems: 'center', gap: space.md }}>
        <CrestSeal height={48} opacity={0.5} />
        <Text style={[type.pageTitle, { color: c.textPrimary, textAlign: 'center' }]}>Sign in</Text>
        <Text style={[type.body, { color: c.textSecondary, textAlign: 'center' }]}>
          Sign in to QueenZone with Google, Microsoft, Discord, GitHub or Apple. The app never sees a
          password or provider secret.
        </Text>
      </View>
      {error ? (
        <Text style={[type.body, { color: c.danger }]} accessibilityRole="alert">
          {error}
        </Text>
      ) : null}
      <View style={{ gap: 10 }}>
        {providers.map((provider) => (
          <Button
            key={provider.id}
            label={provider.label}
            variant="outline"
            loading={busyProvider === provider.id}
            disabled={busyProvider !== null && busyProvider !== provider.id}
            onPress={() => {
              void onProvider(provider);
            }}
          />
        ))}
      </View>
    </ScrollView>
  );
}
