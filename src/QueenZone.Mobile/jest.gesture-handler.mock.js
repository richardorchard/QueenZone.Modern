const React = require('react');
const { View } = require('react-native');

const chain = () => {
  const handler = {
    onBegin: () => handler,
    onUpdate: () => handler,
    onEnd: () => handler,
    onTouchesDown: () => handler,
    onTouchesMove: () => handler,
    manualActivation: () => handler,
    numberOfTaps: () => handler,
    maxDuration: () => handler,
  };
  return handler;
};

const passthrough = ({ children }) => React.createElement(View, null, children);

module.exports = {
  GestureHandlerRootView: passthrough,
  GestureDetector: passthrough,
  Gesture: {
    Pinch: chain,
    Pan: chain,
    Tap: chain,
    Simultaneous: (...gestures) => gestures[0],
    Exclusive: (...gestures) => gestures[0],
  },
};
