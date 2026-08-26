import { Image } from 'expo-image';
import { useEffect, useMemo, useRef, useState } from 'react';
import {
  AccessibilityInfo,
  Animated,
  Dimensions,
  Easing,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { media } from '../content/media';
import { archiveDisclaimer, dark, motion } from '../theme/tokens';

/**
 * In-app boot splash — covers the gap between the native cold-start splash
 * (expo-splash-screen, `assets/splash-icon.png`) and the first app frame.
 * Mirrors the design handoff at `design/splash-screen/Splash Screen.dc.html`.
 */
type Props = {
  /** Cinzel is loaded async; falls back to a serif system face until then. */
  fontsReady: boolean;
  /** Set true once boot is ready — starts the 320ms fade-out. */
  fadingOut: boolean;
};

const EASING = Easing.bezier(0.22, 0.61, 0.36, 1);
const { width: screenWidth } = Dimensions.get('window');
const LOADER_WIDTH = 96;
const LOADER_SEGMENT = LOADER_WIDTH * 0.45;
/** crest-emblem.png is 395×331 — kept in step so the watermark never distorts. */
const CREST_ASPECT_RATIO = 331 / 395;
const watermarkWidth = screenWidth * 1.44;
const watermarkHeight = watermarkWidth * CREST_ASPECT_RATIO;

export function BootSplash({ fontsReady, fadingOut }: Props) {
  const [reducedMotion, setReducedMotion] = useState(false);
  const overlayOpacity = useRef(new Animated.Value(1)).current;
  const watermark = useRef(new Animated.Value(0)).current;
  const crestRise = useRef(new Animated.Value(0)).current;
  const wordmarkRise = useRef(new Animated.Value(0)).current;
  const footerFade = useRef(new Animated.Value(0)).current;
  const loaderWipe = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    let cancelled = false;
    AccessibilityInfo.isReduceMotionEnabled?.()
      .then((enabled) => {
        if (!cancelled) setReducedMotion(Boolean(enabled));
      })
      .catch(() => {
        /* default to full motion when the check is unavailable */
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const dur = (ms: number) => (reducedMotion ? 1 : ms);
    Animated.parallel([
      Animated.timing(watermark, {
        toValue: 1,
        duration: dur(1200),
        easing: EASING,
        useNativeDriver: true,
      }),
      Animated.timing(crestRise, {
        toValue: 1,
        duration: dur(900),
        delay: dur(120),
        easing: EASING,
        useNativeDriver: true,
      }),
      Animated.timing(wordmarkRise, {
        toValue: 1,
        duration: dur(900),
        delay: dur(380),
        easing: EASING,
        useNativeDriver: true,
      }),
      Animated.timing(footerFade, {
        toValue: 1,
        duration: dur(900),
        delay: dur(700),
        easing: EASING,
        useNativeDriver: true,
      }),
    ]).start();
  }, [reducedMotion, watermark, crestRise, wordmarkRise, footerFade]);

  useEffect(() => {
    if (reducedMotion) return undefined;
    const loop = Animated.loop(
      Animated.timing(loaderWipe, {
        toValue: 1,
        duration: 1700,
        easing: EASING,
        useNativeDriver: true,
      }),
    );
    loop.start();
    return () => loop.stop();
  }, [reducedMotion, loaderWipe]);

  useEffect(() => {
    if (!fadingOut) return;
    Animated.timing(overlayOpacity, {
      toValue: 0,
      duration: reducedMotion ? 1 : motion.base,
      easing: EASING,
      useNativeDriver: true,
    }).start();
  }, [fadingOut, reducedMotion, overlayOpacity]);

  const rise = (value: Animated.Value) => ({
    opacity: value,
    transform: [{ translateY: value.interpolate({ inputRange: [0, 1], outputRange: [10, 0] }) }],
  });

  const titleFontFamily = useMemo(
    () => (fontsReady ? 'Cinzel-Regular' : 'Georgia'),
    [fontsReady],
  );

  const loaderTranslateX = loaderWipe.interpolate({
    inputRange: [0, 1],
    outputRange: [-LOADER_SEGMENT, LOADER_WIDTH + LOADER_SEGMENT * 1.2],
  });

  return (
    <Animated.View
      pointerEvents="none"
      style={[styles.overlay, { opacity: overlayOpacity }]}
      testID="boot-splash"
    >
      <Animated.Image
        source={media.crestEmblem}
        accessibilityElementsHidden
        importantForAccessibility="no"
        style={[
          styles.watermark,
          {
            width: watermarkWidth,
            height: watermarkHeight,
            marginLeft: -watermarkWidth / 2,
            marginTop: -watermarkHeight / 2,
            opacity: watermark.interpolate({ inputRange: [0, 1], outputRange: [0, dark.crestWatermarkOpacity] }),
          },
        ]}
        resizeMode="contain"
      />

      <View style={styles.center}>
        <Animated.View style={rise(crestRise)}>
          <Image
            source={media.crestEmblem}
            style={styles.crest}
            contentFit="contain"
            accessibilityElementsHidden
            importantForAccessibility="no"
          />
        </Animated.View>

        <Animated.View style={[styles.wordmarkGroup, rise(wordmarkRise)]}>
          <Text style={[styles.wordmark, { fontFamily: titleFontFamily }]}>Queenzone</Text>
          <View style={styles.divider} />
          <Text style={[styles.tagline, { fontFamily: titleFontFamily }]}>
            The Queenzone.com Archive
          </Text>
        </Animated.View>
      </View>

      <Animated.View style={[styles.footer, { opacity: footerFade }]}>
        <View style={styles.loaderTrack}>
          <Animated.View
            style={[
              styles.loaderFill,
              { width: LOADER_SEGMENT, transform: [{ translateX: loaderTranslateX }] },
            ]}
          />
        </View>
        <Text style={styles.disclaimer}>{archiveDisclaimer}</Text>
      </Animated.View>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: dark.surfacePage,
    alignItems: 'center',
    justifyContent: 'space-between',
    zIndex: 100,
  },
  watermark: {
    position: 'absolute',
    top: '50%',
    left: '50%',
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 28,
    paddingHorizontal: 40,
  },
  crest: {
    width: 128,
    height: 107,
  },
  wordmarkGroup: {
    alignItems: 'center',
    gap: 18,
  },
  wordmark: {
    fontSize: 27,
    letterSpacing: 7,
    lineHeight: 27,
    color: dark.textPrimary,
    textTransform: 'uppercase',
  },
  divider: {
    width: 44,
    height: 1,
    backgroundColor: dark.borderStrong,
  },
  tagline: {
    fontSize: 9.5,
    letterSpacing: 3,
    color: 'rgba(255,255,255,0.6)',
    textTransform: 'uppercase',
    textAlign: 'center',
  },
  footer: {
    alignItems: 'center',
    gap: 22,
    paddingHorizontal: 40,
    paddingBottom: 46,
  },
  loaderTrack: {
    width: LOADER_WIDTH,
    height: 1,
    backgroundColor: 'rgba(255,255,255,0.14)',
    overflow: 'hidden',
  },
  loaderFill: {
    height: 1,
    backgroundColor: 'rgba(255,255,255,0.75)',
  },
  disclaimer: {
    fontFamily: 'Inter-Regular',
    fontSize: 10.5,
    lineHeight: 16.5,
    letterSpacing: 0.2,
    color: 'rgba(255,255,255,0.38)',
    textAlign: 'center',
    maxWidth: 260,
  },
});
