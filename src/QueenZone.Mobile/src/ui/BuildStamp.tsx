import { StyleSheet, Text, View } from 'react-native';
import { getAppConfig } from '../config/appConfig';
import { formatBuildStamp } from '../config/buildMetadata';
import { space, type, useTheme } from '../theme';

export function BuildStamp() {
  const { c } = useTheme();
  const text = formatBuildStamp(getAppConfig());

  if (!text) {
    return null;
  }

  return (
    <View style={[styles.container, { borderTopColor: c.hairline }]}>
      <Text style={[type.caption, styles.text, { color: c.textMuted }]}>{text}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    borderTopWidth: StyleSheet.hairlineWidth,
    marginTop: space.section,
    paddingTop: space.md,
    paddingBottom: space.base,
  },
  text: {
    fontSize: 12,
    letterSpacing: 0.2,
    textAlign: 'center',
  },
});
