import { useCallback, useEffect, useLayoutEffect } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ApiError, fetchQuoteById } from '../../api';
import { useDetailQuery } from '../../hooks/useDetailQuery';
import { HeaderBackButton } from '../../navigation/headerButtons';
import type { HomeStackParamList } from '../../navigation/types';
import { LoadingBlock } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import { space, type, useTheme } from '../../theme';

type Props = NativeStackScreenProps<HomeStackParamList, 'Quote'>;

function quoteContext(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

export function QuoteScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { id } = route.params;
  const loadQuote = useCallback(
    (signal: AbortSignal) => {
      if (!Number.isInteger(id) || id <= 0) {
        return Promise.reject(new ApiError(404, 'Not Found'));
      }
      return fetchQuoteById(id, signal);
    },
    [id],
  );
  const { data: quote, error, loading } = useDetailQuery(loadQuote);

  useLayoutEffect(() => {
    navigation.setOptions({
      title: 'Queen Quotes',
      headerLeft: () => (
        <HeaderBackButton
          testID={testIds.quoteBack}
          onPress={() => {
            if (navigation.canGoBack()) {
              navigation.goBack();
              return;
            }
            navigation.navigate('Home');
          }}
        />
      ),
    });
  }, [navigation]);

  useEffect(() => {
    if (!Number.isInteger(id) || id <= 0) {
      navigation.replace('Home');
      return;
    }
    if (!loading && (error || !quote)) {
      navigation.replace('Home');
    }
  }, [error, id, loading, navigation, quote]);

  if (loading || error || !quote) {
    return <LoadingBlock label="Loading quote…" />;
  }

  const context = quoteContext(quote.context);

  return (
    <ScrollView
      testID={testIds.quoteScreen}
      style={[styles.scroll, { backgroundColor: c.surfacePage }]}
      contentContainerStyle={styles.content}
    >
      <Text style={[type.eyebrow, { color: c.accentEditorial }]}>Queen Quotes</Text>
      <Text
        style={[type.articleTitle, { color: c.textPrimary, marginTop: space.sm }]}
        allowFontScaling
        maxFontSizeMultiplier={1.4}
      >
        “{quote.text}”
      </Text>
      <Text style={[type.body, { color: c.textMuted, marginTop: space.md }]}>— {quote.whoSaid}</Text>
      {context ? (
        <View testID={testIds.quoteContext} style={styles.context}>
          <Text style={[type.eyebrow, { color: c.accentEditorial }]}>Context</Text>
          <Text style={[type.body, { color: c.textSecondary, marginTop: space.sm }]}>{context}</Text>
        </View>
      ) : null}
      <View style={{ height: space.section }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scroll: { flex: 1 },
  content: {
    paddingHorizontal: 26,
    paddingTop: space.xl,
    paddingBottom: space.section,
  },
  context: {
    marginTop: space.xxl,
  },
});
