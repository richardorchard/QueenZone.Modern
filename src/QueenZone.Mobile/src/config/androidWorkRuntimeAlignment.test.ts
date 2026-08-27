import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { applyWorkRuntimeAlignment } = require('../../plugins/withAndroidWorkRuntimeAlignment.cjs') as {
  applyWorkRuntimeAlignment: (contents: string) => string;
};

describe('applyWorkRuntimeAlignment', () => {
  it('appends a WorkManager force block once', () => {
    const first = applyWorkRuntimeAlignment('// root gradle\n');
    assert.match(first, /force "androidx.work:work-runtime:2.8.1"/);
    assert.match(first, /force "androidx.work:work-runtime-ktx:2.8.1"/);

    const second = applyWorkRuntimeAlignment(first);
    assert.equal(second, first);
  });
});
