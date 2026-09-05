type Stopper = () => void;

let stopper: Stopper | null = null;
let currentPerformanceId: string | null = null;

export function registerPlaybackStopper(next: Stopper | null): () => void {
  stopper = next;
  return () => {
    if (stopper === next) {
      stopper = null;
    }
  };
}

export function setActivePlaybackId(performanceId: string | null): void {
  currentPerformanceId = performanceId;
}

export function stopActivePlayback(): void {
  stopper?.();
}

export function stopPlaybackIf(performanceId: string): void {
  if (currentPerformanceId === performanceId) {
    stopActivePlayback();
  }
}
