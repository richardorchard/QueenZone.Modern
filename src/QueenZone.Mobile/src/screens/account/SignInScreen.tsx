import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';

export function SignInScreen() {
  const { signIn } = useSession();

  return (
    <PlaceholderScreen
      title="Sign in"
      epic="Epic 6 — Account"
      access="public"
      description="Placeholder for the mobile OAuth2 PKCE flow from Epic 0. The development toggle signs in locally until that client is wired."
      actions={[{ label: 'Sign in (development)', onPress: signIn }]}
    />
  );
}
