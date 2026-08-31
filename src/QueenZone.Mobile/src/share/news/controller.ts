import { createNewsSuggestion } from '../../api/newsSuggestions';
import { createNewsShareStore } from './draftStore';
import { createNewsShareController, type NewsShareController } from './session';

let sharedController: NewsShareController | null = null;

function createDefaultStore() {
  // eslint-disable-next-line @typescript-eslint/no-require-imports -- lazy so Node unit tests never load the native AsyncStorage binding.
  const AsyncStorage = require('@react-native-async-storage/async-storage').default as {
    getItem(key: string): Promise<string | null>;
    setItem(key: string, value: string): Promise<void>;
    removeItem(key: string): Promise<void>;
  };
  return createNewsShareStore(AsyncStorage);
}

export function getNewsShareController(): NewsShareController {
  if (!sharedController) {
    sharedController = createNewsShareController(createDefaultStore(), createNewsSuggestion);
  }
  return sharedController;
}

export function resetNewsShareController(controller?: NewsShareController | null): void {
  sharedController = controller ?? null;
}
