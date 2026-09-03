import { Children, isValidElement, type ReactNode } from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SearchRouteScreen } from '../screens/archive/SearchScreen';
import { StoryScreen } from '../screens/archive/StoryScreen';
import { NewsStoryScreen } from '../screens/news/NewsStoryScreen';
import { dark, light } from '../theme';
import { commonScreens, stackScreenOptions } from './stacks';
import type { CommonStackParamList, StoryRouteParamList } from './types';

type SharedParams = CommonStackParamList & StoryRouteParamList;
const Stack = createNativeStackNavigator<SharedParams>();

function screenEntries(node: ReactNode): { name: string; component: unknown }[] {
  return Children.toArray(node).flatMap((child) => {
    if (!isValidElement(child)) {
      return [];
    }
    const props = child.props as { name?: string; component?: unknown; children?: ReactNode };
    if (typeof props.name === 'string') {
      return [{ name: props.name, component: props.component }];
    }
    return screenEntries(props.children);
  });
}

function registered(options?: { story?: 'news' | 'archive' }) {
  return screenEntries(commonScreens(Stack, options));
}

describe('commonScreens', () => {
  it('always registers SearchRouteScreen and omits Story when story is unset', () => {
    expect(registered()).toEqual([{ name: 'Search', component: SearchRouteScreen }]);
  });

  it('registers NewsStoryScreen when story is news', () => {
    expect(registered({ story: 'news' })).toEqual([
      { name: 'Search', component: SearchRouteScreen },
      { name: 'Story', component: NewsStoryScreen },
    ]);
  });

  it('registers StoryScreen when story is archive', () => {
    expect(registered({ story: 'archive' })).toEqual([
      { name: 'Search', component: SearchRouteScreen },
      { name: 'Story', component: StoryScreen },
    ]);
  });
});

describe('stackScreenOptions', () => {
  it('derives header chrome from the given colour scheme', () => {
    const darkOptions = stackScreenOptions(dark);
    expect(darkOptions.headerStyle).toEqual({ backgroundColor: dark.surfacePage });
    expect(darkOptions.headerTintColor).toBe(dark.accentPrimary);
    expect(darkOptions.headerTitleStyle).toEqual({
      color: dark.textPrimary,
      fontWeight: '600',
    });
    expect(darkOptions.headerShadowVisible).toBe(false);
    expect(darkOptions.contentStyle).toEqual({ backgroundColor: dark.surfacePage });

    const lightOptions = stackScreenOptions(light);
    expect(lightOptions.headerStyle).toEqual({ backgroundColor: light.surfacePage });
    expect(lightOptions.headerTintColor).toBe(light.accentPrimary);
    expect(lightOptions.contentStyle).toEqual({ backgroundColor: light.surfacePage });
    expect(lightOptions.headerStyle).not.toEqual(darkOptions.headerStyle);
  });
});
