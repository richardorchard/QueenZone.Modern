import { StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { space, type, useTheme } from '../theme';

type Props = {
  eyebrow: string;
  title: string;
  /** When the native stack header is hidden, pad for the status bar. */
  headerShown?: boolean;
};

export function ListScreenHeader({ eyebrow, title, headerShown = true }: Props) {
  const insets = useSafeAreaInsets();
  const { c } = useTheme();

  return (
    <View
      style={[
        styles.header,
        { paddingTop: headerShown ? space.xl : insets.top + space.xl },
      ]}
    >
      <Text style={[type.eyebrow, { color: c.accentPrimary }]}>{eyebrow}</Text>
      <Text
        style={[type.pageTitle, { color: c.textPrimary, marginTop: space.sm }]}
        maxFontSizeMultiplier={1.4}
        allowFontScaling
      >
        {title}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  header: {
    paddingHorizontal: space.xl,
    paddingBottom: space.base,
  },
});
