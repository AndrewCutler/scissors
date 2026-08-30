import { describe, expect, it, vi } from 'vitest';

describe('clippings hub connection', () => {
	it('configures the hub connection with the expected url and token factory', async () => {
		const state: {
			url?: string;
			options?: { accessTokenFactory?: () => Promise<string> | string };
			logging?: unknown;
			automaticReconnect?: boolean;
		} = {};
		const connection = { id: 'connection' };

		class FakeHubConnectionBuilder {
			withUrl(url: string, options: { accessTokenFactory: () => Promise<string> | string }) {
				state.url = url;
				state.options = options;
				return this;
			}

			withAutomaticReconnect() {
				state.automaticReconnect = true;
				return this;
			}

			configureLogging(level: unknown) {
				state.logging = level;
				return this;
			}

			build() {
				return connection;
			}
		}

		vi.resetModules();
		vi.doMock('@microsoft/signalr', () => ({
			default: {},
			HubConnectionBuilder: FakeHubConnectionBuilder,
			LogLevel: { Information: 'Information' },
		}));

		const { createClippingsHubConnection } = await import('./clippingsHub');
		const result = createClippingsHubConnection(() => 'access-token');

		expect(result).toBe(connection);
		expect(state.url).toBe('http://10.0.2.2:5098/clippingsHub');
		expect(state.automaticReconnect).toBe(true);
		expect(state.logging).toBe('Information');
		await expect(state.options!.accessTokenFactory!()).resolves.toBe('access-token');
	});

	it('falls back to an empty token when none is available', async () => {
		const state: {
			options?: { accessTokenFactory?: () => Promise<string> | string };
		} = {};

		class FakeHubConnectionBuilder {
			withUrl(_url: string, options: { accessTokenFactory: () => Promise<string> | string }) {
				state.options = options;
				return this;
			}

			withAutomaticReconnect() {
				return this;
			}

			configureLogging() {
				return this;
			}

			build() {
				return {};
			}
		}

		vi.resetModules();
		vi.doMock('@microsoft/signalr', () => ({
			default: {},
			HubConnectionBuilder: FakeHubConnectionBuilder,
			LogLevel: { Information: 'Information' },
		}));

		const { createClippingsHubConnection } = await import('./clippingsHub');
		createClippingsHubConnection(() => undefined);

		await expect(state.options!.accessTokenFactory!()).resolves.toBe('');
	});
});
