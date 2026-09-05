import {
	getOrCreateDeviceIdAsync,
	getRefreshTokenAsync,
} from 'src/util/storage';
import {
	Clipping,
	GoogleWebAuthResponse,
	GoogleWebAuthResponseDTO,
} from './models';
import { isWeb } from 'src/util/isMobile';
import { apiUrl } from './config';
import { Platform } from 'react-native';
import { Result } from 'src/util/result.model';

class ApiResponseError extends Error {
	constructor(methodName: string, response: Response) {
		super(
			`Failed to complete ${methodName}: code ${response.status}, text ${response.statusText}`,
		);
		this.name = 'ApiResponseError';
	}
}

export const completeGoogleAuth = async (
	idToken: string,
): Promise<Result<GoogleWebAuthResponse>> => {
	try {
		const deviceId = await getOrCreateDeviceIdAsync();
		const response = await fetch(
			// TODO: when web is implemented, switch route on platform
			apiUrl('/auth/google/mobile'),
			{
				body: JSON.stringify({
					idToken,
					deviceId,
					platform:
						// TODO: fix backend DTO
						Platform.OS === 'android'
							? 3
							: Platform.OS === 'ios'
								? 2
								: Platform.OS === 'web'
									? 1
									: undefined,
				}),
				method: 'POST',
				headers: {
					Accept: 'application/json',
					'Content-Type': 'application/json',
				},
			},
		);

		if (response.status != 200) {
			throw new ApiResponseError(completeGoogleAuth.name, response);
		}

		const {
			accessToken,
			accessTokenExpiresAt,
			refreshToken,
		}: GoogleWebAuthResponseDTO = await response.json();
		const expiresAtTimestamp = new Date(accessTokenExpiresAt).getTime();

		return {
			value: {
				accessToken,
				accessTokenExpiresAt: expiresAtTimestamp,
				refreshToken,
			},
			success: true,
		};
	} catch (e) {
		console.error(completeGoogleAuth.name, e);

		return { success: false };
	}
};

export const refreshSession = async (
	abortController?: AbortController,
): Promise<Result<GoogleWebAuthResponse>> => {
	try {
		let url = apiUrl('/auth/refresh/');
		if (isWeb) {
			url += 'web';
		} else {
			url += 'mobile';
		}

		const rt = await getRefreshTokenAsync();
		const deviceId = await getOrCreateDeviceIdAsync();

		const response = await fetch(url, {
			method: 'POST',
			body: JSON.stringify({
				refreshToken: rt,
				deviceId,
				platform:
					// TODO: fix backend DTO
					Platform.OS === 'android'
						? 3
						: Platform.OS === 'ios'
							? 2
							: Platform.OS === 'web'
								? 1
								: undefined,
			}),
			headers: {
				Accept: 'application/json',
				'Content-Type': 'application/json',
			},
			signal: abortController?.signal,
		});

		if (response.status === 401) {
			console.log(refreshSession.name, 'not authenticated.');
			return { success: false, error: 'Refresh token failed' };
		}

		if (response.status !== 200) {
			throw new ApiResponseError(refreshSession.name, response);
		}

		const {
			accessToken,
			accessTokenExpiresAt,
			refreshToken,
		}: GoogleWebAuthResponseDTO = await response.json();
		const expiresAtTimestamp = new Date(accessTokenExpiresAt).getTime();

		return {
			value: {
				accessToken,
				accessTokenExpiresAt: expiresAtTimestamp,
				refreshToken,
			},
			success: true,
		};
	} catch (e) {
		console.error(refreshSession.name, e);

		return { success: false };
	}
};

export const getClippings = async (
	accessToken: string,
): Promise<Result<Clipping[]>> => {
	try {
		const response = await fetch(apiUrl('/clippings'), {
			method: 'GET',
			headers: {
				Accept: 'application/json',
				'Content-Type': 'application/json',
				Authorization: `Bearer ${accessToken}`,
			},
		});

		if (response.status !== 200) {
			throw new ApiResponseError(getClippings.name, response);
		}

		const data: Clipping[] = await response.json();

		const sorted = data.sort((a, b) =>
			a.capturedAt > b.capturedAt ? 1 : -1,
		);

		return { value: sorted, success: true };
	} catch (e) {
		console.error(getClippings.name, e);

		return { success: false };
	}
};
