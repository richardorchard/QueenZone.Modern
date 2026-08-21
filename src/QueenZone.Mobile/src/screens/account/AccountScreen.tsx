import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { CompositeScreenProps } from '@react-navigation/native';
import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { getAppConfig } from '../../config/appConfig';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';
import type { RootTabParamList, YouStackParamList } from '../../navigation/types';

type Props = CompositeScreenProps<
  NativeStackScreenProps<YouStackParamList, 'Account'>,
  BottomTabScreenProps<RootTabParamList>
>;

export function AccountScreen({ navigation }: Props) {
  const { isSignedIn, displayName, signIn, signOut } = useSession();
  const { appEnv, apiBaseUrl } = getAppConfig();
  const apiLine = `API ${appEnv} → ${apiBaseUrl}`;

  if (isSignedIn) {
    return (
      <PlaceholderScreen
        title={displayName ?? 'You'}
        epic="Epic 6 — Account"
        access="member"
        headerShown={false}
        description={`Signed-in account hub. Profile, settings, and sign-out live here. Private messages have their own tab once you are signed in. ${apiLine}`}
        actions={[
          { label: 'Profile', onPress: () => navigation.navigate('Profile'), variant: 'outline' },
          { label: 'Settings', onPress: () => navigation.navigate('Settings'), variant: 'outline' },
          { label: 'Help', onPress: () => navigation.navigate('Help'), variant: 'ghost' },
          { label: 'Sign out (development)', onPress: signOut, variant: 'outline' },
        ]}
      />
    );
  }

  return (
    <PlaceholderScreen
      title="You"
      epic="Epic 6 — Account"
      access="public"
      headerShown={false}
      description={`Signed-out account tab. Visitors can sign in or send a Help request. Member profile and settings stay behind sign-in. ${apiLine}`}
      actions={[
        { label: 'Sign in (development)', onPress: signIn },
        { label: 'Help', onPress: () => navigation.navigate('Help'), variant: 'outline' },
      ]}
    />
  );
}
