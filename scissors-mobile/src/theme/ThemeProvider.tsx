import {
	createContext,
	PropsWithChildren,
	SetStateAction,
	useContext,
	useState,
} from 'react';
import { darkTheme, lightTheme, Theme, ThemeMode } from './theme';
import { useColorScheme } from 'react-native';

type ThemeContextType = {
	theme: Theme;
	mode: ThemeMode;
	setMode: React.Dispatch<SetStateAction<ThemeMode>>;
};

const ThemeContext = createContext<ThemeContextType>({
	theme: darkTheme,
	mode: 'system',
	setMode: () => undefined,
});

const ThemeProvider = ({ children }: PropsWithChildren) => {
	const systemScheme = useColorScheme();
	const [mode, setMode] = useState<ThemeMode>('system');

	const resolvedMode = mode === 'system' ? (systemScheme ?? 'light') : mode;
	const theme = resolvedMode === 'dark' ? darkTheme : lightTheme;

	return (
		<ThemeContext.Provider value={{ theme, mode, setMode }}>
			{children}
		</ThemeContext.Provider>
	);
};

export default ThemeProvider;

export const useTheme = () => useContext(ThemeContext);
