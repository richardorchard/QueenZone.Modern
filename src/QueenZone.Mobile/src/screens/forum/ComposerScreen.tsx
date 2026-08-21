import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';

export function ComposerScreen() {
  return (
    <MemberGate title="Compose">
      <PlaceholderScreen
        title="Compose"
        epic="Epic 2 — Forum"
        access="member"
        description="Member thread composer placeholder."
      />
    </MemberGate>
  );
}
