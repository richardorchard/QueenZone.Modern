const React = require('react');
const { View } = require('react-native');

const recorded = {
  pinch: null,
  pan: null,
  tap: null,
};

function chain(kind) {
  const handlers = {};
  const gesture = {
    onBegin(fn) {
      handlers.onBegin = fn;
      return gesture;
    },
    onUpdate(fn) {
      handlers.onUpdate = fn;
      return gesture;
    },
    onEnd(fn) {
      handlers.onEnd = fn;
      return gesture;
    },
    onTouchesDown(fn) {
      handlers.onTouchesDown = fn;
      return gesture;
    },
    onTouchesMove(fn) {
      handlers.onTouchesMove = fn;
      return gesture;
    },
    manualActivation() {
      return gesture;
    },
    numberOfTaps() {
      return gesture;
    },
    maxDuration() {
      return gesture;
    },
    handlers,
  };
  recorded[kind] = gesture;
  return gesture;
}

const passthrough = ({ children }) => React.createElement(View, null, children);

module.exports = {
  GestureHandlerRootView: passthrough,
  GestureDetector: passthrough,
  Gesture: {
    Pinch: () => chain('pinch'),
    Pan: () => chain('pan'),
    Tap: () => chain('tap'),
    Simultaneous: (...gestures) => gestures[0],
    Exclusive: (...gestures) => gestures[0],
  },
  getRecordedGestures: () => recorded,
};
