import type {
  ForumStackParamList,
  SignInParams,
  SignInReturnTo,
} from '../navigation/types';

type NavigateAction = {
  type: 'NAVIGATE';
  payload: { name: 'SignIn'; params?: SignInParams };
};

type DispatchNav = {
  dispatch: (action: NavigateAction) => void;
};

function navigateToSignIn(params?: SignInParams): NavigateAction {
  return { type: 'NAVIGATE', payload: { name: 'SignIn', params } };
}

export function tabsNavigateParams(returnTo: SignInReturnTo): {
  screen: SignInReturnTo['tab'];
  params: { screen: string; params?: object };
} {
  return {
    screen: returnTo.tab,
    params:
      returnTo.params === undefined
        ? { screen: returnTo.screen }
        : { screen: returnTo.screen, params: returnTo.params },
  };
}

export function openSignIn(navigation: DispatchNav, returnTo?: SignInReturnTo): void {
  navigation.dispatch(navigateToSignIn(returnTo ? { returnTo } : undefined));
}

export function completeSignInNavigation(
  navigation: {
    navigate: (name: 'Tabs', params: ReturnType<typeof tabsNavigateParams>) => void;
    goBack: () => void;
    canGoBack: () => boolean;
  },
  returnTo?: SignInReturnTo,
): void {
  if (returnTo) {
    navigation.navigate('Tabs', tabsNavigateParams(returnTo));
    return;
  }

  if (navigation.canGoBack()) {
    navigation.goBack();
    return;
  }

  navigation.navigate('Tabs', tabsNavigateParams({ tab: 'HomeTab', screen: 'Profile' }));
}

export function openForumComposer(
  navigation: {
    navigate: (name: 'Composer', params: ForumStackParamList['Composer']) => void;
    dispatch: DispatchNav['dispatch'];
  },
  isSignedIn: boolean,
  params: ForumStackParamList['Composer'],
): void {
  if (isSignedIn) {
    navigation.navigate('Composer', params);
    return;
  }

  openSignIn(navigation, { tab: 'ForumTab', screen: 'Composer', params });
}

export function openPhotoSubmit(
  navigation: DispatchNav,
  isSignedIn: boolean,
  signedInNavigate: () => void,
): void {
  if (isSignedIn) {
    signedInNavigate();
    return;
  }

  openSignIn(navigation, { tab: 'PhotosTab', screen: 'PhotoSubmit' });
}
