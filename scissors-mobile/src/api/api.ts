import { getRefreshTokenAsync } from 'src/util/storage';
import {
	Clipping,
	GoogleWebAuthResponse,
	GoogleWebAuthResponseDTO,
} from './models';
import { isWeb } from 'src/util/isMobile';

export const completeGoogleAuth = async (
	idToken: string,
): Promise<GoogleWebAuthResponse | undefined> => {
	try {
		const response = await fetch(
			process.env.EXPO_PUBLIC_API_URL + '/auth/google/web',
			{
				body: JSON.stringify({ idToken }),
				method: 'POST',
				headers: {
					Accept: 'application/json',
					'Content-Type': 'application/json',
				},
			},
		);

		const {
			accessToken,
			accessTokenExpiresAt,
			refreshToken,
		}: GoogleWebAuthResponseDTO = await response.json();
		const expiresAtTimestamp = new Date(accessTokenExpiresAt).getTime();

		return {
			accessToken,
			accessTokenExpiresAt: expiresAtTimestamp,
			refreshToken,
		};
	} catch (e) {
		console.error(completeGoogleAuth.name, e);
	}
};

export const refreshSession = async (
	abortController?: AbortController,
): Promise<GoogleWebAuthResponse | undefined> => {
	try {
		let url = process.env.EXPO_PUBLIC_API_URL + '/auth/refresh/';
		if (isWeb) {
			url += 'web';
		} else {
			url += 'native';
		}

		const rt = await getRefreshTokenAsync();

		console.log({ rt });

		const response = await fetch(url, {
			method: 'POST',
			body: JSON.stringify({ refreshToken: rt }),
			headers: {
				Accept: 'application/json',
				'Content-Type': 'application/json',
			},
			signal: abortController?.signal,
		});

		console.log({ response });

		if (response.status === 401) {
			return undefined;
		}

		const {
			accessToken,
			accessTokenExpiresAt,
			refreshToken,
		}: GoogleWebAuthResponseDTO = await response.json();
		const expiresAtTimestamp = new Date(accessTokenExpiresAt).getTime();

		return {
			accessToken,
			accessTokenExpiresAt: expiresAtTimestamp,
			refreshToken,
		};
	} catch (e) {
		console.error(refreshSession.name, e);
	}
};

export const getClippings = async (
	accessToken: string,
): Promise<Clipping[] | undefined> => {
	try {
		const response = await fetch(
			process.env.EXPO_PUBLIC_API_URL + '/clippings',
			{
				method: 'GET',
				headers: {
					Accept: 'application/json',
					'Content-Type': 'application/json',
					Authorization: `Bearer ${accessToken}`,
				},
			},
		);

		const data: Clipping[] = await response.json();

		const sorted = data.sort((a, b) =>
			a.capturedAt > b.capturedAt ? 1 : -1,
		);

		return sorted;
	} catch (e) {
		console.error(getClippings.name, e);
	}
};
