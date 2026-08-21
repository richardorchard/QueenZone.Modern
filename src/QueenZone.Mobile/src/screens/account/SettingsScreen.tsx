import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';

export function SettingsScreen() {
  return (
    <MemberGate title="Settings">
      <PlaceholderScreen
        title="Settings"
        epic="Epic 6 — Account"
        access="member"
        description="Member settings placeholder, including sign-out and account deletion later in Epic 6."
      />
    </MemberGate>
  );
}
