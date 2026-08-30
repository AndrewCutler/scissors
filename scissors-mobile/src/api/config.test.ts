import { afterEach, describe, expect, it, vi } from 'vitest';

describe('api config', () => {
	afterEach(() => {
		vi.unstubAllEnvs();
		vi.resetModules();
	});

	it('trims trailing slashes from the configured API base url', async () => {
		vi.stubEnv('EXPO_PUBLIC_API_URL', 'https://example.com/api/v1///');

		const { API_BASE_URL, apiUrl } = await import('./config');

		expect(API_BASE_URL).toBe('https://example.com/api/v1');
		expect(apiUrl('/clippings')).toBe('https://example.com/api/v1/clippings');
	});

	it('derives the SignalR base url from the api url when possible', async () => {
		vi.stubEnv('EXPO_PUBLIC_API_URL', 'https://example.com/api/v1/');

		const { SIGNALR_BASE_URL, signalRUrl } = await import('./config');

		expect(SIGNALR_BASE_URL).toBe('https://example.com');
		expect(signalRUrl('/clippingsHub')).toBe('https://example.com/clippingsHub');
	});
});
