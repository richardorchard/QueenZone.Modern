import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';

export function ComposeMessageScreen() {
  return (
    <MemberGate title="New message">
      <PlaceholderScreen
        title="New message"
        epic="Epic 3 — Private messaging"
        access="member"
        description="Member compose placeholder."
      />
    </MemberGate>
  );
}
