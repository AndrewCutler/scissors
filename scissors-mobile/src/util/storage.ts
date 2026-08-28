import * as SecureStore from 'expo-secure-store';
import { isMobile } from './isMobile';

const REFRESH_TOKEN_KEY = 'refreshToken' as const;

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
