import { createContext } from 'react';
import { Clipping } from 'src/api/models';

export type AppContextType = {
	auth: {
		expiresAt?: number;
		accessToken?: string;
		isAuthenticated?: boolean;
		user?: any;
	};
	setExpiresAt: (e: number) => void;
	setAccessToken: (t: string) => void;
	setUser: (u?: any) => void;
	setClippings: (c: Clipping[]) => void;
	clippings: Clipping[];
};

export const AppContext = createContext<AppContextType>({
	setAccessToken: () => undefined,
	setExpiresAt: () => undefined,
	setUser: () => undefined,
	setClippings: () => undefined,
	auth: {},
	clippings: [],
});
