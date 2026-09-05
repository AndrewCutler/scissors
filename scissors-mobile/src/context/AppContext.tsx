import { createContext, SetStateAction } from 'react';
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
	setClippings: React.Dispatch<SetStateAction<Clipping[]>>;
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
