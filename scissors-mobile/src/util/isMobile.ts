import { Platform } from 'react-native';

export const isMobile = ['android', 'ios'].includes(Platform.OS);
export const isWeb = Platform.OS === 'web';
