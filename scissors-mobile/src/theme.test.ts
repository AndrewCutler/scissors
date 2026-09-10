/*
 * CODEX-MODIFIED: the contents of this file were written by a human and modified after the fact by a Codex agent.
*/
import { describe, expect, it } from 'vitest';
import { darkTheme, lightTheme } from './theme/theme';

describe('theme', () => {
	it('uses light surfaces and dark text in light mode', () => {
		expect(lightTheme.colors).toMatchObject({
			background: '#F8F6F3',
			surface: '#FFFFFF',
			text: '#171717',
		});
	});

	it('uses dark surfaces and light text in dark mode', () => {
		expect(darkTheme.colors).toMatchObject({
			background: '#151718',
			surface: '#202223',
			text: '#F5F3F0',
		});
	});

	it('preserves shared accents and layout tokens across modes', () => {
		for (const theme of [lightTheme, darkTheme]) {
			expect(theme.colors.primary).toBe('#C89A73');
			expect(theme.colors.primaryStrong).toBe('#c5864f');
			expect(theme.radius.pill).toBe(999);
		}
		expect(darkTheme.spacing).toEqual(lightTheme.spacing);
		expect(darkTheme.radius).toEqual(lightTheme.radius);
	});
});
