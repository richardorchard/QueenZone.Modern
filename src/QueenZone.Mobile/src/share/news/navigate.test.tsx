import { nestedTabParams } from '../../navigation/nestedTab';
import { openSuggestNewsScreen } from './navigate';

describe('openSuggestNewsScreen', () => {
  it('stays on the Home stack when SuggestNews is already a route', () => {
    const navigate = jest.fn();
    openSuggestNewsScreen({
      navigate,
      getState: () => ({ routeNames: ['Home', 'SuggestNews'] }),
    });
    expect(navigate).toHaveBeenCalledWith('SuggestNews');
  });

  it('pushes through the current tab navigator', () => {
    const navigate = jest.fn();
    openSuggestNewsScreen({
      navigate,
      getState: () => ({ routeNames: ['HomeTab', 'NewsTab', 'PhotosTab'] }),
    });
    expect(navigate).toHaveBeenCalledWith('HomeTab', nestedTabParams('SuggestNews'));
  });

  it('opens Tabs from a root stack that is not already on a tab', () => {
    const navigate = jest.fn();
    openSuggestNewsScreen({
      navigate,
      getState: () => ({ routeNames: ['Tabs', 'SignIn'] }),
    });
    expect(navigate).toHaveBeenCalledWith('Tabs', {
      screen: 'HomeTab',
      params: nestedTabParams('SuggestNews'),
    });
  });
});
