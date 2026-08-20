import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';

export function PhotoSubmitScreen() {
  return (
    <MemberGate title="Submit a photo">
      <PlaceholderScreen
        title="Submit a photo"
        epic="Epic 4 — Photo galleries"
        access="member"
        description="Member photo submission placeholder. Camera and library access come later in Epic 4."
      />
    </MemberGate>
  );
}
