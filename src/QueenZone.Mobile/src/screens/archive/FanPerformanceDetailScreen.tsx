import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { MemberGate } from '../../session/MemberGate';

export function FanPerformanceDetailScreen() {
  return (
    <MemberGate title="Stream audio">
      <PlaceholderScreen
        title="Fan performance"
        epic="Epic 5 — Fan performances"
        access="member"
        description="Member-only audio stream placeholder. Background playback and lock-screen controls come later in Epic 5."
      />
    </MemberGate>
  );
}
