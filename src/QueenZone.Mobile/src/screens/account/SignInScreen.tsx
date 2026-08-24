import { useCallback, useEffect, useRef, useState } from 'react';
import { ScrollView, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { getAppConfig } from '../../config/appConfig';
import { authProvidersUrl, fallbackAuthProviders, parseAuthProviders, type AuthProvider } from '../../api/auth';
import type { RootStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { completeSignInNavigation } from '../../session/signInNavigation';
import { space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { CrestSeal } from '../../ui/CrestSeal';

type Props = NativeStackScreenProps<RootStackParamList, 'SignIn'>;

export function SignInScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { isSignedIn, signIn } = useSession();
  const [providers, setProviders] = useState<AuthProvider[]>(fallbackAuthProviders);
  const [busyProvider, setBusyProvider] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const finished = useRef(false);

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

  const leaveAfterSignIn = useCallback(() => {
    if (finished.current) {
      return;
    }
    finished.current = true;
    completeSignInNavigation(navigation as never, route.params?.returnTo);
  }, [navigation, route.params?.returnTo]);

  useEffect(() => {
    if (isSignedIn) {
      leaveAfterSignIn();
    }
  }, [isSignedIn, leaveAfterSignIn]);

  const onProvider = useCallback(
    async (provider: AuthProvider) => {
      if (busyProvider) {
        return;
      }

      setError(null);
      setBusyProvider(provider.id);
      try {
        await signIn(provider.id);
        leaveAfterSignIn();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Could not sign in.');
      } finally {
        setBusyProvider(null);
      }
    },
    [busyProvider, leaveAfterSignIn, signIn],
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
