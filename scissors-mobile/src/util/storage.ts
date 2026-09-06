import * as SecureStore from 'expo-secure-store';
import { isMobile } from './isMobile';
import { createUniqueId } from './unique-id';

const REFRESH_TOKEN_KEY = 'refreshToken' as const;
const DEVICE_ID_KEY = 'deviceId' as const;

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

	const deviceId = createUniqueId();
	await SecureStore.setItemAsync(DEVICE_ID_KEY, deviceId);
	return deviceId;
};
