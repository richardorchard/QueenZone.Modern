const { View } = require('react-native');

function useSharedValue(init) {
  return { value: init };
}

function useAnimatedStyle(updater) {
  return typeof updater === 'function' ? updater() : updater;
}

function withSpring(toValue) {
  return toValue;
}

function runOnJS(fn) {
  return fn;
}

const AnimatedView = View;

module.exports = {
  __esModule: true,
  default: { View: AnimatedView, createAnimatedComponent: (component) => component },
  View: AnimatedView,
  useSharedValue,
  useAnimatedStyle,
  withSpring,
  runOnJS,
};
