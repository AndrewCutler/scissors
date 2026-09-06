export type GoogleWebAuthResponseDTO = {
	accessToken: string;
	accessTokenExpiresAt: string;
	refreshToken?: string;
};

export type GoogleWebAuthResponse = Omit<
	GoogleWebAuthResponseDTO,
	'accessTokenExpiresAt'
> & {
	accessTokenExpiresAt: number;
};

export type Clipping =
	| {
			id: number;
			text: string;
			capturedAt: Date;
			hasServerId: true;
	  }
	| {
			temporaryId: string;
			text: string;
			capturedAt: Date;
			hasServerId: false;
	  };

export type ServerClipping = Extract<Clipping, { hasServerId: true }>;

export type ClientClipping = Extract<Clipping, { hasServerId: false }>;

export type GetClippingDTO = Pick<ServerClipping, 'id' | 'text' | 'capturedAt'>;
