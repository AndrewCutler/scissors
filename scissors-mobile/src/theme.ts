export const theme = {
	colors: {
		background: '#F8F6F3',
		surface: '#FFFFFF',
		surfaceStrong: '#F1EEEA',
		border: '#171717',
		text: '#171717',
		textMuted: '#AAA7A3',
		primary: '#C89A73',
		primaryStrong: '#c5864f',
		secondary: '#74716E',
		success: '#17A668',
		danger: '#B64D47',
	},
	spacing: {
		xs: 6,
		sm: 10,
		md: 16,
		lg: 24,
		xl: 32,
	},
	radius: {
		md: 16,
		lg: 22,
		pill: 999,
	},
} as const;

export const darkTheme = {
	...theme,
	colors: {
        ...theme.colors,
		background: '#151718',
		surface: '#202223',
		surfaceStrong: '#292B2C',
		text: '#F5F3F0',
		textMuted: '#74716E',
		secondary: '#AAA7A3',
	},
};
