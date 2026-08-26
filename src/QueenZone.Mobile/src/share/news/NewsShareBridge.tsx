import { useEffect, useRef } from 'react';
import { useNavigation } from '@react-navigation/native';
import { useShareIntent } from 'expo-share-intent';
import { useSession } from '../../session/SessionContext';
import { getNewsShareController } from './controller';
import { openSuggestNewsScreen, type SuggestNewsNav } from './navigate';
import type { ShareRaw } from './parseShare';
import { planShareRestore } from './restorePlan';

function toShareRaw(intent: { text?: string | null; webUrl?: string | null; files?: unknown[] | null }): ShareRaw {
  return {
    text: intent.text,
    webUrl: intent.webUrl,
    hasFiles: Array.isArray(intent.files) && intent.files.length > 0,
  };
}

export function NewsShareBridge() {
  const navigation = useNavigation<SuggestNewsNav>();
  const { isRestoring } = useSession();
  const { hasShareIntent, shareIntent, resetShareIntent } = useShareIntent({
    resetOnBackground: true,
    disabled: false,
  });
  const navigationRef = useRef(navigation);
  const restoringRef = useRef(isRestoring);
  const pendingOpen = useRef(false);
  const didHydrate = useRef(false);
  const wasRestoring = useRef(isRestoring);

  navigationRef.current = navigation;
  restoringRef.current = isRestoring;

  useEffect(() => {
    if (!hasShareIntent) {
      return;
    }

    const controller = getNewsShareController();
    void (async () => {
      await controller.capture(toShareRaw(shareIntent));
      resetShareIntent();
      if (restoringRef.current) {
        pendingOpen.current = controller.view().kind !== 'idle';
        return;
      }
      if (controller.view().kind !== 'idle') {
        openSuggestNewsScreen(navigationRef.current);
      }
    })();
  }, [hasShareIntent, shareIntent, resetShareIntent]);

  useEffect(() => {
    const finishedRestore = wasRestoring.current && !isRestoring;
    wasRestoring.current = isRestoring;
    const plan = planShareRestore({
      isRestoring,
      pendingOpen: pendingOpen.current,
      didHydrate: didHydrate.current,
      finishedRestore,
    });

    if (plan === 'wait' || plan === 'noop') {
      return;
    }

    const controller = getNewsShareController();
    void (async () => {
      if (plan === 'openCaptured') {
        pendingOpen.current = false;
        didHydrate.current = true;
      } else {
        await controller.hydrate();
        didHydrate.current = true;
        pendingOpen.current = false;
      }

      if (controller.view().kind !== 'idle') {
        openSuggestNewsScreen(navigationRef.current);
      }
    })();
  }, [isRestoring]);

  return null;
}
