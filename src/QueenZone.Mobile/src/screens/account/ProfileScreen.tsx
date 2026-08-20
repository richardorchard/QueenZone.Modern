import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';

export function ProfileScreen() {
  return (
    <MemberGate title="Profile">
      <PlaceholderScreen
        title="Profile"
        epic="Epic 6 — Account"
        access="member"
        description="Member profile placeholder."
      />
    </MemberGate>
  );
}
