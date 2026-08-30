import * as signalR from '@microsoft/signalr';
import { SIGNALR_BASE_URL } from './config';
import { Clipping } from './models';

export const createClippingsHubConnection = (
	getAccessToken: () => string | undefined,
): signalR.HubConnection => {
	return new signalR.HubConnectionBuilder()
		.withUrl(`${SIGNALR_BASE_URL}/clippingsHub`, {
			accessTokenFactory: async () => getAccessToken() ?? '',
		})
		.withAutomaticReconnect()
		.configureLogging(signalR.LogLevel.Information)
		.build();
};

export type NewClippingHandler = (clipping: Clipping) => void;
