import { createContext } from 'react';

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
};

export const AppContext = createContext<AppContextType>({
	setAccessToken: () => undefined,
	setExpiresAt: () => undefined,
	setUser: () => undefined,
	auth: {},
});
