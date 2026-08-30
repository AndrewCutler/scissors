import { describe, expect, it, vi } from 'vitest';

async function loadStorage(platform: string) {
	vi.resetModules();
	const secureStore = {
		setItemAsync: vi.fn(),
		getItemAsync: vi.fn().mockResolvedValue('stored-refresh-token'),
		deleteItemAsync: vi.fn(),
	};

	vi.doMock('react-native', () => ({
		Platform: { OS: platform },
	}));
	vi.doMock('expo-secure-store', () => secureStore);

	const storage = await import('./storage');

	return { storage, secureStore };
}

describe('refresh token storage', () => {
	it('persists refresh tokens on mobile platforms', async () => {
		const { storage, secureStore } = await loadStorage('android');

		await storage.setRefreshTokenAsync('token');
		await storage.getRefreshTokenAsync();
		await storage.deleteRefreshTokenAsync();

		expect(secureStore.setItemAsync).toHaveBeenCalledWith('refreshToken', 'token');
		expect(secureStore.getItemAsync).toHaveBeenCalledWith('refreshToken');
		expect(secureStore.deleteItemAsync).toHaveBeenCalledWith('refreshToken');
	});

	it('becomes a no-op on web', async () => {
		const { storage, secureStore } = await loadStorage('web');

		await storage.setRefreshTokenAsync('token');
		const value = await storage.getRefreshTokenAsync();
		await storage.deleteRefreshTokenAsync();

		expect(value).toBeUndefined();
		expect(secureStore.setItemAsync).not.toHaveBeenCalled();
		expect(secureStore.getItemAsync).not.toHaveBeenCalled();
		expect(secureStore.deleteItemAsync).not.toHaveBeenCalled();
	});
});
