import { getNetworkStateAsync } from 'expo-network';

/** True when the device is definitely offline. Unknown stays online. */
export async function detectOffline(): Promise<boolean> {
  try {
    const state = await getNetworkStateAsync();
    return state.isInternetReachable === false || state.isConnected === false;
  } catch {
    return false;
  }
}
