import type { ReactNode } from 'react';
import { PlaceholderScreen } from '../ui/PlaceholderScreen';
import { useSession } from './SessionContext';

type Props = {
  title: string;
  children: ReactNode;
};

export function MemberGate({ title, children }: Props) {
  const { isSignedIn, signIn } = useSession();

  if (isSignedIn) {
    return children;
  }

  return (
    <PlaceholderScreen
      title={title}
      epic="Members"
      access="member"
      description="This area matches the website's member-only boundary. Sign in to continue. Token auth will replace this development toggle."
      actions={[{ label: 'Sign in (development)', onPress: signIn }]}
    />
  );
}
