import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { FanPerformanceDownloadsList } from '../../downloads/FanPerformanceDownloadsList';
import type { ArchiveStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'FanPerformanceDownloads'>;

export function FanPerformanceDownloadsScreen(_props: Props) {
  return (
    <MemberGate title="Downloads">
      <FanPerformanceDownloadsList />
    </MemberGate>
  );
}
