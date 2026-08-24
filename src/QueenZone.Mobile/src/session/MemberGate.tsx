import type { ReactNode } from 'react';
import { useNavigation } from '@react-navigation/native';
import { PlaceholderScreen } from '../ui/PlaceholderScreen';
import { useSession } from './SessionContext';
import { openSignIn } from './signInNavigation';

type Props = {
  title: string;
  children: ReactNode;
};

export function MemberGate({ title, children }: Props) {
  const { isSignedIn, isRestoring } = useSession();
  const navigation = useNavigation();

  if (isRestoring) {
    return null;
  }

  if (isSignedIn) {
    return children;
  }

  return (
    <PlaceholderScreen
      title={title}
      epic="Members"
      access="member"
      description="This area matches the website's member-only boundary. Sign in with Google, Microsoft, Discord, GitHub, or Apple to continue."
      actions={[
        {
          label: 'Sign in',
          onPress: () => openSignIn(navigation),
        },
      ]}
    />
  );
}
