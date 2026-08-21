import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';
import type { MessagesStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MessagesStackParamList, 'Inbox'>;

export function InboxScreen({ navigation }: Props) {
  return (
    <MemberGate title="Messages">
      <PlaceholderScreen
        title="Messages"
        epic="Epic 3 — Private messaging"
        access="member"
        headerShown={false}
        description="Member inbox placeholder. This tab is omitted while signed out, matching the website header."
        actions={[
          {
            label: 'Open a conversation',
            onPress: () => navigation.navigate('Conversation', { id: 'sample' }),
            variant: 'outline',
          },
          { label: 'Compose', onPress: () => navigation.navigate('ComposeMessage') },
        ]}
      />
    </MemberGate>
  );
}
