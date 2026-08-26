import { useCallback, useEffect, useState } from 'react';
import { getNewsShareController } from './controller';
import { openSuggestNewsScreen, type SuggestNewsNav } from './navigate';
import type { NewsShareView } from './session';

export type { NewsShareView } from './session';
export type { SuggestNewsNav } from './navigate';
export { NewsShareBridge } from './NewsShareBridge';
export { getNewsShareController, resetNewsShareController } from './controller';
export { openSuggestNewsScreen } from './navigate';

export function openSuggestNews(navigation: SuggestNewsNav): void {
  const controller = getNewsShareController();
  void controller.openBlank().then(() => {
    openSuggestNewsScreen(navigation);
  });
}

export function useNewsShare(): NewsShareView {
  const controller = getNewsShareController();
  const [view, setView] = useState(() => controller.view());

  const refresh = useCallback(() => {
    setView(controller.view());
  }, [controller]);

  useEffect(() => controller.subscribe(refresh), [controller, refresh]);

  return view;
}
