import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';
import type { ForumStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<ForumStackParamList, 'Thread'>;

export function ThreadScreen({ navigation, route }: Props) {
  const { isSignedIn } = useSession();

  return (
    <PlaceholderScreen
      title="Thread"
      epic="Epic 2 — Forum"
      access="public"
      description={`Public thread reader placeholder (${route.params.id}). Replies are members-only, matching the website.`}
      actions={[
        isSignedIn
          ? {
              label: 'Reply',
              onPress: () => navigation.navigate('Composer', { threadId: route.params.id }),
            }
          : {
              label: 'Sign in to reply',
              onPress: () => navigation.navigate('Composer', { threadId: route.params.id }),
              variant: 'outline',
            },
      ]}
    />
  );
}
