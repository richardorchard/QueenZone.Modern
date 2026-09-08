import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { testIds } from '../../test/testIds';
import { radius, type, useTheme } from '../../theme';
import { wallpaperCopy, wallpaperTargets, type WallpaperTarget } from './wallpaperMeta';

const targetTestIds: Record<WallpaperTarget, string> = {
  home: testIds.photoViewerWallpaperHome,
  lock: testIds.photoViewerWallpaperLock,
  both: testIds.photoViewerWallpaperBoth,
};

type Props = {
  onSelect: (target: WallpaperTarget) => void;
  onCancel: () => void;
};

export function WallpaperTargetSheet({ onSelect, onCancel }: Props) {
  const insets = useSafeAreaInsets();
  const { c } = useTheme();

  return (
    <View
      testID={testIds.photoViewerWallpaperSheet}
      pointerEvents="box-none"
      style={[StyleSheet.absoluteFill, { justifyContent: 'flex-end' }]}
    >
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Dismiss wallpaper options"
        onPress={onCancel}
        style={[StyleSheet.absoluteFill, { backgroundColor: 'rgba(0,0,0,0.5)' }]}
      />
      <View
        style={{
          backgroundColor: c.surfaceSheet,
          borderTopLeftRadius: radius.sheet,
          borderTopRightRadius: radius.sheet,
          paddingBottom: insets.bottom + 12,
          paddingTop: 8,
        }}
      >
        {wallpaperTargets.map((option) => (
          <Pressable
            key={option.target}
            testID={targetTestIds[option.target]}
            accessibilityRole="button"
            accessibilityLabel={option.label}
            onPress={() => onSelect(option.target)}
            style={{ paddingHorizontal: 24, paddingVertical: 16 }}
          >
            <Text style={[type.body, { color: c.textPrimary }]}>{option.label}</Text>
          </Pressable>
        ))}
        <Pressable
          testID={testIds.photoViewerWallpaperCancel}
          accessibilityRole="button"
          accessibilityLabel={wallpaperCopy.cancel}
          onPress={onCancel}
          style={{ paddingHorizontal: 24, paddingVertical: 16 }}
        >
          <Text style={[type.body, { color: c.textMuted }]}>{wallpaperCopy.cancel}</Text>
        </Pressable>
      </View>
    </View>
  );
}
