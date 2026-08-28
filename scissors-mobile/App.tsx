import { StatusBar } from 'expo-status-bar';
import { StyleSheet, View } from 'react-native';

import { HomeScreen } from './src/screens/HomeScreen';
import { useEffect, useState } from 'react';
import { AppContext, AppContextType } from 'src/context/AppContext';
import { Clipping } from 'src/api/models';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { theme } from 'src/theme';
import { refreshSession } from 'src/api/api';
import { setRefreshTokenAsync } from 'src/util/storage';

export default function App() {
	const [auth, setAuth] = useState<AppContextType['auth']>({});

	const setUser = (user?: any): void => {
		setAuth((prev) => ({ ...prev, user }));
	};

	const setExpiresAt = (expiresAt: number): void => {
		setAuth((prev) => ({ ...prev, expiresAt }));
	};

	const setAccessToken = (accessToken: string): void => {
		setAuth((prev) => ({ ...prev, accessToken }));
	};

	const [clippings, setClippings] = useState<Clipping[]>([]);

	const isAuthenticated =
		!!auth.accessToken && !!auth.expiresAt && auth.expiresAt > Date.now();

	useEffect(() => {
		const controller = new AbortController();
		try {
			const request = async () => {
				const response = await refreshSession(controller);
				if (response) {
					await setRefreshTokenAsync(response?.refreshToken);
				} else {
					console.log('Refresh failed; not authenticated.');
				}
			};

			request();
		} catch (e) {
			console.error(e);
		}

		return () => controller.abort();
	}, []);

	return (
		<SafeAreaProvider>
			<AppContext.Provider
				value={{
					auth: {
						...auth,
						isAuthenticated,
					},
					setUser,
					setAccessToken,
					setExpiresAt,
					clippings,
					setClippings,
				}}
			>
				<SafeAreaView
					style={styles.root}
					edges={['top', 'left', 'right']}
				>
					<StatusBar style="dark" />
					<View pointerEvents="none" style={styles.glowTop} />
					<View pointerEvents="none" style={styles.glowBottom} />
					<HomeScreen />
				</SafeAreaView>
			</AppContext.Provider>
		</SafeAreaProvider>
	);
}

const styles = StyleSheet.create({
	root: {
		flex: 1,
		backgroundColor: theme.colors.background,
	},
	glowTop: {
		position: 'absolute',
		top: -120,
		left: -80,
		width: 260,
		height: 260,
		borderRadius: 260,
		backgroundColor: 'rgba(74, 125, 204, 0.16)',
	},
	glowBottom: {
		position: 'absolute',
		right: -110,
		bottom: -120,
		width: 320,
		height: 320,
		borderRadius: 320,
		backgroundColor: 'rgba(141, 98, 66, 0.18)',
	},
});
