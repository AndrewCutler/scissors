import { StatusBar } from 'expo-status-bar';
import { SafeAreaView, StyleSheet, View } from 'react-native';

import { HomeScreen } from './src/screens/HomeScreen';

export default function App() {
  return (
    <SafeAreaView style={styles.root}>
      <StatusBar style="light" />
      <View pointerEvents="none" style={styles.glowTop} />
      <View pointerEvents="none" style={styles.glowBottom} />
      <HomeScreen />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: '#0C1018'
  },
  glowTop: {
    position: 'absolute',
    top: -120,
    left: -80,
    width: 260,
    height: 260,
    borderRadius: 260,
    backgroundColor: 'rgba(89, 164, 255, 0.18)'
  },
  glowBottom: {
    position: 'absolute',
    right: -110,
    bottom: -120,
    width: 320,
    height: 320,
    borderRadius: 320,
    backgroundColor: 'rgba(255, 126, 103, 0.16)'
  }
});
