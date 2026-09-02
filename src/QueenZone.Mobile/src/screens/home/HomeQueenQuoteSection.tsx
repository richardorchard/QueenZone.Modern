import { memo } from 'react';
import { FeatureBlock } from '../../ui/FeatureBlock';
import { testIds } from '../../test/testIds';
import { queenQuotesEyebrow, type HomeQuote } from './homeMeta';

export const HomeQueenQuoteSection = memo(function HomeQueenQuoteSection({
  quote,
  quoteId,
  onOpenQuote,
}: {
  quote: HomeQuote;
  quoteId: number;
  onOpenQuote: (id: number) => void;
}) {
  return (
    <FeatureBlock
      testID={testIds.homeQuote}
      eyebrow={queenQuotesEyebrow()}
      quote={quote}
      onPress={quoteId > 0 ? () => onOpenQuote(quoteId) : undefined}
    />
  );
});
