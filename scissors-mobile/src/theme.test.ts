import { describe, expect, it } from 'vitest';
import { theme } from './theme';

describe('theme', () => {
	it('keeps the warm earthy color palette', () => {
		expect(theme.colors.background).toBe('#C89A73');
		expect(theme.colors.surface).toBe('#E7D3BF');
		expect(theme.colors.primaryStrong).toBe('#2E67C8');
		expect(theme.radius.pill).toBe(999);
	});
});
