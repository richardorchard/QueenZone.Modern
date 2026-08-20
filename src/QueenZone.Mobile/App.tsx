import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View } from 'react-native';

export default function App() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>QueenZone</Text>
      <Text style={styles.subtitle}>Development build scaffold</Text>
      <StatusBar style="light" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#111111',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  title: {
    color: '#FFFFFF',
    fontSize: 28,
    fontWeight: '600',
  },
  subtitle: {
    color: '#E8E8E8',
    fontSize: 16,
    marginTop: 8,
    textAlign: 'center',
  },
});
