import { Clipping, GoogleWebAuthResponse, GoogleWebAuthResponseDTO } from './models';

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

		const { accessToken, accessTokenExpiresAt }: GoogleWebAuthResponseDTO =
			await response.json();
		const expiresAtTimestamp = new Date(accessTokenExpiresAt).getTime();

		return {
			accessToken,
			accessTokenExpiresAt: expiresAtTimestamp,
		};
	} catch (e) {
		console.error(e);
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

		const data = await response.json();

		return data;
	} catch (e) {
		console.error(e);
	}
};
