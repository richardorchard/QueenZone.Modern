import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { regionFromLocale, sectionFromNavigationState } from './catalog.ts';

describe('analytics catalogue', () => {
  it('reports only the active top-level section', () => {
    assert.equal(
      sectionFromNavigationState({
        index: 0,
        routes: [
          {
            name: 'Tabs',
            state: {
              index: 1,
              routes: [
                { name: 'HomeTab' },
                { name: 'NewsTab', state: { index: 0, routes: [{ name: 'Story' }] } },
              ],
            },
          },
        ],
      }),
      'news',
    );
  });

  it('does not expose modal, detail, or unknown route names', () => {
    assert.equal(
      sectionFromNavigationState({ index: 1, routes: [{ name: 'Tabs' }, { name: 'SignIn' }] }),
      null,
    );
    assert.equal(sectionFromNavigationState({ routes: [{ name: 'Story' }] }), null);
  });

  it('extracts only a two-letter device region', () => {
    assert.equal(regionFromLocale('en-AU'), 'AU');
    assert.equal(regionFromLocale('de_DE'), 'DE');
    assert.equal(regionFromLocale('en'), null);
    assert.equal(regionFromLocale('not a locale'), null);
  });
});
