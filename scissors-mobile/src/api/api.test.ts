/*
 * CODEX-MODIFIED: the contents of this file were written by a human and modified after the fact by a Codex agent.
 */

import { describe, expect, it, vi } from 'vitest';

async function loadApi(options: { isWeb?: boolean; refreshToken?: string | null; deviceId?: string | null } = {}) {
	vi.resetModules();

	const getRefreshTokenAsync = vi.fn().mockResolvedValue(options.refreshToken ?? 'refresh-token');
	const getOrCreateDeviceIdAsync = vi.fn().mockResolvedValue(options.deviceId ?? 'device-id');
	vi.doMock('src/util/storage', () => ({
		getRefreshTokenAsync,
		getOrCreateDeviceIdAsync,
	}));
	vi.doMock('src/util/isMobile', () => ({
		isWeb: options.isWeb ?? false,
	}));
	vi.doMock('react-native', () => ({
		Platform: { OS: options.isWeb ? 'web' : 'android' },
	}));

	const api = await import('./api');

	return { api, getRefreshTokenAsync, getOrCreateDeviceIdAsync };
}

describe('api client', () => {
	it('completes google auth and normalizes the expiration timestamp', async () => {
		const response = {
			accessToken: 'access-token',
			accessTokenExpiresAt: '2026-08-30T12:34:56.000Z',
			refreshToken: 'refresh-token',
		};
		const fetchSpy = vi.fn().mockResolvedValue(new Response(JSON.stringify(response), { status: 200 }));
		vi.stubGlobal('fetch', fetchSpy);

		const { api } = await loadApi();
		const result = await api.completeGoogleAuth('id-token');

		expect(fetchSpy).toHaveBeenCalledWith(
			'http://10.0.2.2:5098/api/v1/auth/google/mobile',
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify({
					idToken: 'id-token',
					deviceId: 'device-id',
					platform: 3,
				}),
			}),
		);
		expect(result).toEqual({
			value: {
				accessToken: 'access-token',
				accessTokenExpiresAt: Date.parse('2026-08-30T12:34:56.000Z'),
				refreshToken: 'refresh-token',
			},
			success: true,
		});
	});

	it('refreshes the web session when the api accepts the refresh token', async () => {
		const response = {
			accessToken: 'new-access-token',
			accessTokenExpiresAt: '2026-08-30T13:00:00.000Z',
			refreshToken: 'new-refresh-token',
		};
		const fetchSpy = vi.fn().mockResolvedValue(new Response(JSON.stringify(response), { status: 200 }));
		vi.stubGlobal('fetch', fetchSpy);

		const { api, getRefreshTokenAsync, getOrCreateDeviceIdAsync } = await loadApi({ isWeb: true });
		const result = await api.refreshSession();

		expect(getRefreshTokenAsync).toHaveBeenCalled();
		expect(getOrCreateDeviceIdAsync).toHaveBeenCalled();
		expect(fetchSpy).toHaveBeenCalledWith(
			'http://10.0.2.2:5098/api/v1/auth/refresh/web',
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify({
					refreshToken: 'refresh-token',
					deviceId: 'device-id',
					platform: 1,
				}),
			}),
		);
		expect(result).toEqual({
			value: {
				accessToken: 'new-access-token',
				accessTokenExpiresAt: Date.parse('2026-08-30T13:00:00.000Z'),
				refreshToken: 'new-refresh-token',
			},
			success: true,
		});
	});

	it('returns a failed result when the refresh endpoint rejects the token', async () => {
		const fetchSpy = vi.fn().mockResolvedValue(new Response('', { status: 401 }));
		vi.stubGlobal('fetch', fetchSpy);

		const { api } = await loadApi();
		const result = await api.refreshSession();

		expect(result).toEqual({
			success: false,
			error: 'Refresh token failed',
		});
	});

	it('returns a failed result for an unsuccessful google auth response', async () => {
		const fetchSpy = vi.fn().mockResolvedValue(new Response('', { status: 500 }));
		vi.stubGlobal('fetch', fetchSpy);

		const { api } = await loadApi();
		const result = await api.completeGoogleAuth('id-token');

		expect(result).toEqual({ success: false });
	});

	it('returns the clippings sorted by captured time', async () => {
		const response = [
			{ id: 2, text: 'newer', capturedAt: '2026-08-30T13:00:00.000Z', createdAt: '2026-08-30T13:00:00.000Z' },
			{ id: 1, text: 'older', capturedAt: '2026-08-30T12:00:00.000Z', createdAt: '2026-08-30T12:00:00.000Z' },
		];
		const fetchSpy = vi.fn().mockResolvedValue(new Response(JSON.stringify(response), { status: 200 }));
		vi.stubGlobal('fetch', fetchSpy);

		const { api } = await loadApi();
		const result = await api.getClippings('access-token');

		expect(fetchSpy).toHaveBeenCalledWith(
			'http://10.0.2.2:5098/api/v1/clippings',
			expect.objectContaining({
				headers: expect.objectContaining({
					Authorization: 'Bearer access-token',
				}),
			}),
		);
		expect(result.success).toBe(true);
		if (result.success) {
			expect(result.value.map((item) => item.id)).toEqual([1, 2]);
		}
	});
});
