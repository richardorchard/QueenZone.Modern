import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';
import type { ForumStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<ForumStackParamList, 'ForumIndex'>;

export function ForumScreen({ navigation }: Props) {
  const { isSignedIn } = useSession();

  return (
    <PlaceholderScreen
      title="Forum"
      epic="Epic 2 — Forum"
      access="public"
      headerShown={false}
      description="Forum browsing is public, matching /forum. Starting a thread or posting a reply requires a signed-in member."
      actions={[
        {
          label: 'Open a thread',
          onPress: () => navigation.navigate('Thread', { id: 'sample' }),
          variant: 'outline',
        },
        isSignedIn
          ? { label: 'New thread', onPress: () => navigation.navigate('Composer', {}) }
          : { label: 'Sign in to post', onPress: () => navigation.navigate('Composer', {}), variant: 'ghost' },
      ]}
    />
  );
}
