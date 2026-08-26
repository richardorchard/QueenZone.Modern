import { nestedTabParams } from '../../navigation/nestedTab';

export type SuggestNewsNav = {
  navigate: (name: string, params?: object) => void;
  getState?: () => { routeNames?: readonly string[] } | undefined;
};

export function openSuggestNewsScreen(navigation: SuggestNewsNav): void {
  const routeNames = navigation.getState?.()?.routeNames ?? [];
  if (routeNames.includes('SuggestNews')) {
    navigation.navigate('SuggestNews');
    return;
  }

  if (routeNames.includes('HomeTab') || routeNames.includes('NewsTab') || routeNames.includes('PhotosTab')) {
    navigation.navigate('HomeTab', nestedTabParams('SuggestNews'));
    return;
  }

  navigation.navigate('Tabs', {
    screen: 'HomeTab',
    params: nestedTabParams('SuggestNews'),
  });
}
