import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';
import type { HomeStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<HomeStackParamList, 'Inbox'>;

export function InboxScreen({ navigation }: Props) {
  return (
    <MemberGate title="Messages">
      <PlaceholderScreen
        title="Messages"
        epic="Epic 3 — Private messaging"
        access="member"
        headerShown={false}
        description="Member inbox. Private messages live behind the Home masthead profile, not a sixth tab."
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
