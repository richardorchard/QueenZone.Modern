import 'react-native-gesture-handler';
import * as Sentry from '@sentry/react-native';
import { registerRootComponent } from 'expo';
import { registerWidgetTaskHandler } from 'react-native-android-widget';

import App from './App';
import { initSentry } from './src/config/sentry';
import {
  defineHomeWidgetBackgroundTask,
  registerHomeWidgetBackgroundRefresh,
} from './src/widgets/widgetSync';
import { widgetTaskHandler } from './src/widgets/widgetTaskHandler';

initSentry();

// defineTask must run in the global scope so iOS can wake JS overnight (#990).
defineHomeWidgetBackgroundTask();
void registerHomeWidgetBackgroundRefresh();

// registerRootComponent calls AppRegistry.registerComponent('main', () => App).
registerRootComponent(Sentry.wrap(App));

// Android-only headless task (see widgetTaskHandler.tsx); a no-op registration on iOS.
registerWidgetTaskHandler(widgetTaskHandler);
