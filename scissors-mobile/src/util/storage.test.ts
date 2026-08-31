import { describe, expect, it, vi } from 'vitest';

async function loadStorage(platform: string, storedValue: string | null = 'stored-refresh-token') {
	vi.resetModules();
	const secureStore = {
		setItemAsync: vi.fn(),
		getItemAsync: vi.fn().mockResolvedValue(storedValue),
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

describe('device id storage', () => {
	it('persists a generated device id on mobile platforms', async () => {
		const { storage, secureStore } = await loadStorage('android', null);

		const deviceId = await storage.getOrCreateDeviceIdAsync();

		expect(deviceId).toMatch(
			/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i,
		);
		expect(secureStore.getItemAsync).toHaveBeenCalledWith('deviceId');
		expect(secureStore.setItemAsync).toHaveBeenCalledWith('deviceId', deviceId);
	});

	it('reuses an existing device id on mobile platforms', async () => {
		const { storage, secureStore } = await loadStorage('android');
		secureStore.getItemAsync.mockResolvedValueOnce('existing-device-id');

		const deviceId = await storage.getOrCreateDeviceIdAsync();

		expect(deviceId).toBe('existing-device-id');
		expect(secureStore.setItemAsync).not.toHaveBeenCalled();
	});
});
