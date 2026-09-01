import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { runPhotoZoomOnJS } from './photoZoomGestures.ts';

describe('photo zoom gestures', () => {
  it('forces pinch-style gestures onto the JS thread', () => {
    const calls: boolean[] = [];
    const gesture = {
      runOnJS(enabled: boolean) {
        calls.push(enabled);
        return gesture;
      },
    };

    assert.equal(runPhotoZoomOnJS(gesture), gesture);
    assert.deepEqual(calls, [true]);
  });
});
