import * as Sentry from '@sentry/react-native';
import { registerRootComponent } from 'expo';

import App from './App';
import { initSentry } from './src/config/sentry';

initSentry();

// registerRootComponent calls AppRegistry.registerComponent('main', () => App).
registerRootComponent(Sentry.wrap(App));
