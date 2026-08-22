import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';

export function SignInScreen() {
  const { signIn } = useSession();

  return (
    <PlaceholderScreen
      title="Sign in"
      epic="Epic 6 — Account"
      access="public"
      description="Placeholder for the mobile OAuth2 PKCE flow from Epic 0. The development toggle only marks this session as signed in locally — it does not store a Bearer token. Forum poll vote and close stay unavailable until that client persists an access token."
      actions={[{ label: 'Sign in (development)', onPress: signIn }]}
    />
  );
}
