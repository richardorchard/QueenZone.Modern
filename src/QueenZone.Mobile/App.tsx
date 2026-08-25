import { DarkTheme, DefaultTheme, NavigationContainer } from '@react-navigation/native';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { View } from 'react-native';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { RootNavigator } from './src/navigation/RootNavigator';
import { SessionProvider } from './src/session/SessionContext';
import { FanPerformancePlayerProvider } from './src/audio/FanPerformancePlayer';
import { ThemeProvider, dark, useQueenzoneFonts, useTheme } from './src/theme';

SplashScreen.preventAutoHideAsync().catch(() => {
  /* already prevented or unavailable in tests */
});

function AppNavigation() {
  const { c, mode } = useTheme();
  const base = mode === 'light' ? DefaultTheme : DarkTheme;

  const navigationTheme = {
    ...base,
    colors: {
      ...base.colors,
      primary: c.accentPrimary,
      background: c.surfacePage,
      card: c.surfacePage,
      text: c.textPrimary,
      border: c.hairline,
      notification: c.accentPrimary,
    },
  };

  return (
    <NavigationContainer theme={navigationTheme}>
      <StatusBar style={mode === 'light' ? 'dark' : 'light'} />
      <RootNavigator />
    </NavigationContainer>
  );
}

export default function App() {
  const [fontsLoaded, fontError] = useQueenzoneFonts();

  useEffect(() => {
    if (fontsLoaded || fontError) {
      SplashScreen.hideAsync().catch(() => {
        /* splash already hidden */
      });
    }
  }, [fontsLoaded, fontError]);

  if (!fontsLoaded && !fontError) {
    return <View style={{ flex: 1, backgroundColor: dark.surfacePage }} />;
  }

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <ThemeProvider preference="dark">
          <SessionProvider>
            <FanPerformancePlayerProvider>
              <AppNavigation />
            </FanPerformancePlayerProvider>
          </SessionProvider>
        </ThemeProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}

/** Dark-first page colour for any pre-provider splash / native chrome. */
export const bootstrapBackground = dark.surfacePage;
