export type GoogleWebAuthResponseDTO = {
	accessToken: string;
	accessTokenExpiresAt: string;
};

export type GoogleWebAuthResponse = Omit<
	GoogleWebAuthResponseDTO,
	'accessTokenExpiresAt'
> & {
	accessTokenExpiresAt: number;
};

export type Clipping = {
	id: number;
	text: string;
	capturedAt: Date;
	createdAt: Date;
};
