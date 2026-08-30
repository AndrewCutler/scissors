import { describe, expect, it, vi } from 'vitest';

async function loadFlags(platform: string) {
	vi.resetModules();
	vi.doMock('react-native', () => ({
		Platform: { OS: platform },
	}));

	return import('./isMobile');
}

describe('platform flags', () => {
	it.each([
		['android', true, false],
		['ios', true, false],
		['web', false, true],
		['windows', false, false],
	])('maps %s correctly', async (platform, mobile, web) => {
		const { isMobile, isWeb } = await loadFlags(platform);

		expect(isMobile).toBe(mobile);
		expect(isWeb).toBe(web);
	});
});
