import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';
import type { MessagesStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MessagesStackParamList, 'Conversation'>;

export function ConversationScreen({ route }: Props) {
  return (
    <MemberGate title="Conversation">
      <PlaceholderScreen
        title="Conversation"
        epic="Epic 3 — Private messaging"
        access="member"
        description={`Member conversation placeholder (${route.params.id}).`}
      />
    </MemberGate>
  );
}
