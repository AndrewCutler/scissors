const DEFAULT_API_BASE_URL = 'http://10.0.2.2:5098/api/v1';
const DEFAULT_SIGNALR_BASE_URL = 'http://10.0.2.2:5098';

const trimTrailingSlash = (url: string): string => url.replace(/\/+$/, '');

const stripApiVersionPath = (url: string): string =>
	trimTrailingSlash(url).replace(/\/api\/v1$/, '');

export const API_BASE_URL = trimTrailingSlash(
	process.env.EXPO_PUBLIC_API_URL ?? DEFAULT_API_BASE_URL,
);

export const SIGNALR_BASE_URL = trimTrailingSlash(
	process.env.EXPO_PUBLIC_SIGNALR_URL ??
		stripApiVersionPath(API_BASE_URL) ??
		DEFAULT_SIGNALR_BASE_URL,
);

export const apiUrl = (path: string): string => `${API_BASE_URL}${path}`;
export const signalRUrl = (path: string): string =>
	`${SIGNALR_BASE_URL}${path}`;
