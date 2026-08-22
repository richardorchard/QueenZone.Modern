import type { ReactNode } from 'react';
import { useNavigation } from '@react-navigation/native';
import type { NavigationProp } from '@react-navigation/native';
import { PlaceholderScreen } from '../ui/PlaceholderScreen';
import type { RootTabParamList } from '../navigation/types';
import { useSession } from './SessionContext';

type Props = {
  title: string;
  children: ReactNode;
};

export function MemberGate({ title, children }: Props) {
  const { isSignedIn, isRestoring } = useSession();
  const navigation = useNavigation<NavigationProp<RootTabParamList>>();

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
          onPress: () => navigation.navigate('HomeTab', { screen: 'SignIn' }),
        },
      ]}
    />
  );
}
