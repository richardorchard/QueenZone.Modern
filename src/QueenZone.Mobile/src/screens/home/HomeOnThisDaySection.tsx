import { memo } from 'react';
import type { TimelineEvent } from '../../api/types';
import { FeatureBlock } from '../../ui/FeatureBlock';
import { onThisDayEyebrow } from './homeMeta';

export const HomeOnThisDaySection = memo(function HomeOnThisDaySection({
  event,
  onViewTimeline,
}: {
  event: TimelineEvent;
  onViewTimeline: () => void;
}) {
  return (
    <FeatureBlock
      eyebrow={onThisDayEyebrow()}
      numeral={event.formattedDate.toUpperCase()}
      body={event.summary}
      actionLabel="View timeline"
      onAction={onViewTimeline}
    />
  );
});
