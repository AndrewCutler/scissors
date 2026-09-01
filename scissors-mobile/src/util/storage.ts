import * as SecureStore from 'expo-secure-store';
import { isMobile } from './isMobile';

const REFRESH_TOKEN_KEY = 'refreshToken' as const;
const DEVICE_ID_KEY = 'deviceId' as const;

const createDeviceId = (): string => {
	return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (character) => {
		const randomValue = (Math.random() * 16) | 0;
		const value = character === 'x'
			? randomValue
			: (randomValue & 0x3) | 0x8;

		return value.toString(16);
	});
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
