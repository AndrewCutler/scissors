import * as SecureStore from 'expo-secure-store';
import { v4 as uuidv4 } from 'uuid';
import { isMobile } from './isMobile';

const REFRESH_TOKEN_KEY = 'refreshToken' as const;
const DEVICE_ID_KEY = 'deviceId' as const;

const createDeviceId = (): string => {
	return uuidv4();
};

export const setRefreshTokenAsync = async (
	rt: string | undefined,
): Promise<void> => {
	if (isMobile) {
		await SecureStore.setItemAsync(REFRESH_TOKEN_KEY, rt ?? '');
	}
};

export const getRefreshTokenAsync = async (): Promise<
	string | null | undefined
> => {
	if (isMobile) {
		const rt = await SecureStore.getItemAsync(REFRESH_TOKEN_KEY);

		return rt;
	}
};

export const deleteRefreshTokenAsync = async (): Promise<void> => {
	if (isMobile) {
		await SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY);
	}
};

export const getDeviceIdAsync = async (): Promise<string | null | undefined> => {
	if (isMobile) {
		return SecureStore.getItemAsync(DEVICE_ID_KEY);
	}
};

export const getOrCreateDeviceIdAsync = async (): Promise<string | undefined> => {
	if (!isMobile) {
		return undefined;
	}

	const existing = await getDeviceIdAsync();
	if (existing) {
		return existing;
	}

	const deviceId = createDeviceId();
	await SecureStore.setItemAsync(DEVICE_ID_KEY, deviceId);
	return deviceId;
};
