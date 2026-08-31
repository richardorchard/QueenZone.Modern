import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { newsDecades } from './newsDecades.ts';

describe('newsDecades', () => {
  it('covers ALL plus the 2000s–2020s windows used by fetchNewsPage', () => {
    assert.deepEqual(
      newsDecades.map((row) => [row.label, row.decadeStart]),
      [
        ['ALL', null],
        ['2020s', 2020],
        ['2010s', 2010],
        ['2000s', 2000],
      ],
    );
  });
});
