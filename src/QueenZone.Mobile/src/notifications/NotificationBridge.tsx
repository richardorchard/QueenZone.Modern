import { useEffect, useRef, useState } from 'react';
import { useNavigation } from '@react-navigation/native';
import { useSession } from '../session/SessionContext';
import { openNotificationDestination, type NotificationNavigation } from './deepLink';
import { ForegroundBanner } from './ForegroundBanner';
import { configureForegroundNotificationHandler } from './handler';
import type { ForegroundNotice, NotificationTap } from './subscribe';
import { subscribeNotificationEvents } from './subscribe';
import type { NotificationDestination } from './payload';

const bannerDismissMs = 6000;

export function NotificationBridge() {
  const navigation = useNavigation<NotificationNavigation>();
  const { isRestoring } = useSession();
  const [banner, setBanner] = useState<ForegroundNotice | null>(null);
  const pendingTap = useRef<NotificationDestination | null>(null);
  const navigationRef = useRef(navigation);
  const restoringRef = useRef(isRestoring);

  navigationRef.current = navigation;
  restoringRef.current = isRestoring;

  useEffect(() => {
    configureForegroundNotificationHandler();

    const subscription = subscribeNotificationEvents({
      onTap: (tap: NotificationTap) => {
        if (restoringRef.current) {
          pendingTap.current = tap.destination;
          return;
        }

        openNotificationDestination(navigationRef.current, tap.destination);
      },
      onForeground: (notice) => {
        setBanner(notice);
      },
    });

    return () => subscription.remove();
  }, []);

  useEffect(() => {
    if (isRestoring || pendingTap.current === null) {
      return;
    }

    const destination = pendingTap.current;
    pendingTap.current = null;
    openNotificationDestination(navigation, destination);
  }, [isRestoring, navigation]);

  useEffect(() => {
    if (!banner) {
      return;
    }

    const timer = setTimeout(() => setBanner(null), bannerDismissMs);
    return () => clearTimeout(timer);
  }, [banner]);

  if (!banner) {
    return null;
  }

  return (
    <ForegroundBanner
      title={banner.title}
      body={banner.body}
      destination={banner.destination}
      onPress={() => {
        setBanner(null);
        if (restoringRef.current) {
          pendingTap.current = banner.destination;
          return;
        }
        openNotificationDestination(navigation, banner.destination);
      }}
      onDismiss={() => setBanner(null)}
    />
  );
}
