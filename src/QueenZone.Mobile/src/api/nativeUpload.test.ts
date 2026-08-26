import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { shouldUseNativeMultipartUpload } from './nativeUpload.ts';

describe('shouldUseNativeMultipartUpload', () => {
  it('is true only on React Native outside tests', () => {
    assert.equal(
      shouldUseNativeMultipartUpload({
        xhr: function XMLHttpRequest() {},
        navigatorProduct: 'ReactNative',
        nodeEnv: 'production',
      }),
      true,
    );
  });

  it('stays on fetch during the Jest / Node test suite', () => {
    assert.equal(
      shouldUseNativeMultipartUpload({
        xhr: function XMLHttpRequest() {},
        navigatorProduct: 'ReactNative',
        nodeEnv: 'test',
      }),
      false,
    );
  });

  it('stays on fetch when XMLHttpRequest is missing', () => {
    assert.equal(
      shouldUseNativeMultipartUpload({
        xhr: undefined,
        navigatorProduct: 'ReactNative',
        nodeEnv: 'production',
      }),
      false,
    );
  });
});
