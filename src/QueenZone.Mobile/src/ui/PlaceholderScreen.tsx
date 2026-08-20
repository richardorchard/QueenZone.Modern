import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { archiveDisclaimer, shellColors } from './shell';

export type PlaceholderAction = {
  label: string;
  onPress: () => void;
  variant?: 'primary' | 'outline' | 'ghost';
};

type Props = {
  title: string;
  epic: string;
  access: 'public' | 'member';
  description: string;
  actions?: PlaceholderAction[];
  headerShown?: boolean;
};

export function PlaceholderScreen({
  title,
  epic,
  access,
  description,
  actions = [],
  headerShown = true,
}: Props) {
  const insets = useSafeAreaInsets();

  return (
    <ScrollView
      style={styles.scroll}
      contentContainerStyle={[
        styles.content,
        { paddingTop: headerShown ? 24 : insets.top + 24, paddingBottom: insets.bottom + 32 },
      ]}
    >
      <Text style={styles.eyebrow}>{epic}</Text>
      <Text style={styles.title}>{title}</Text>
      <Text style={styles.access}>{access === 'public' ? 'Public' : 'Members'}</Text>
      <Text style={styles.description}>{description}</Text>
      {actions.length > 0 ? (
        <View style={styles.actions}>
          {actions.map((action) => (
            <Pressable
              key={action.label}
              accessibilityRole="button"
              accessibilityLabel={action.label}
              onPress={action.onPress}
              style={({ pressed }) => [
                styles.button,
                action.variant === 'outline' || action.variant === 'ghost'
                  ? styles.buttonOutline
                  : styles.buttonPrimary,
                action.variant === 'ghost' ? styles.buttonGhost : null,
                pressed ? styles.buttonPressed : null,
              ]}
            >
              <Text
                style={[
                  styles.buttonLabel,
                  action.variant === 'outline' || action.variant === 'ghost'
                    ? styles.buttonLabelOnDark
                    : styles.buttonLabelOnAccent,
                ]}
              >
                {action.label}
              </Text>
            </Pressable>
          ))}
        </View>
      ) : null}
      <Text style={styles.disclaimer}>{archiveDisclaimer}</Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scroll: {
    flex: 1,
    backgroundColor: shellColors.page,
  },
  content: {
    paddingHorizontal: 24,
    gap: 12,
  },
  eyebrow: {
    color: shellColors.accent,
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 2.2,
    textTransform: 'uppercase',
  },
  title: {
    color: shellColors.text,
    fontSize: 32,
    fontWeight: '600',
  },
  access: {
    color: shellColors.textMuted,
    fontSize: 11,
    letterSpacing: 0.85,
    textTransform: 'uppercase',
  },
  description: {
    color: shellColors.textSecondary,
    fontSize: 16,
    lineHeight: 24,
    marginTop: 8,
  },
  actions: {
    gap: 10,
    marginTop: 16,
  },
  button: {
    minHeight: 48,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 16,
    borderRadius: 2,
  },
  buttonPrimary: {
    backgroundColor: shellColors.accent,
  },
  buttonOutline: {
    backgroundColor: 'transparent',
    borderWidth: 1,
    borderColor: shellColors.border,
  },
  buttonGhost: {
    borderWidth: 0,
  },
  buttonPressed: {
    opacity: 0.85,
    transform: [{ translateY: 1 }],
  },
  buttonLabel: {
    fontSize: 12,
    fontWeight: '500',
    letterSpacing: 1.2,
    textTransform: 'uppercase',
  },
  buttonLabelOnAccent: {
    color: shellColors.onAccent,
  },
  buttonLabelOnDark: {
    color: shellColors.accent,
  },
  disclaimer: {
    color: shellColors.textMuted,
    fontSize: 12,
    lineHeight: 18,
    marginTop: 28,
  },
});
