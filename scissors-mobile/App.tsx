import { StatusBar } from 'expo-status-bar';
import { AppState, StyleSheet, View } from 'react-native';

import { HomeScreen } from './src/screens/HomeScreen';
import { useCallback, useEffect, useRef, useState } from 'react';
import { AppContext, AppContextType } from 'src/context/AppContext';
import { Clipping } from 'src/api/models';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { theme } from 'src/theme';
import { getClippings, refreshSession } from 'src/api/api';
import { setRefreshTokenAsync } from 'src/util/storage';
import { createClippingsHubConnection } from 'src/api/clippingsHub';
import { ToastProvider, useToast } from 'react-native-toast-notifications';

const REFRESH_LEAD_TIME_MS = 5 * 60 * 1000;

export default function App() {
	return (
		<ToastProvider duration={5000}>
			<AppShell />
		</ToastProvider>
	);
}

function AppShell() {
	const [clippings, setClippings] = useState<Clipping[]>([]);
	const [auth, setAuth] = useState<AppContextType['auth']>({});
	const toast = useToast();
	const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
	const refreshInFlightRef = useRef(false);

	const setUser = useCallback((user?: any): void => {
		setAuth((prev) => ({ ...prev, user }));
	}, []);

	const setExpiresAt = useCallback((expiresAt: number): void => {
		setAuth((prev) => ({ ...prev, expiresAt }));
	}, []);

	const setAccessToken = useCallback((accessToken: string): void => {
		setAuth((prev) => ({ ...prev, accessToken }));
	}, []);

	const isAuthenticated =
		!!auth.accessToken && !!auth.expiresAt && auth.expiresAt > Date.now();

	const refreshAuthSession = useCallback(
		async (hydrateClippings = false): Promise<boolean> => {
			if (refreshInFlightRef.current) {
				return false;
			}

			refreshInFlightRef.current = true;

			try {
				const controller = new AbortController();
				const refreshResponse = await refreshSession(controller);
				if (!refreshResponse.success) {
					return false;
				}

				const { accessToken, accessTokenExpiresAt, refreshToken } =
					refreshResponse.value;
				setAccessToken(accessToken);
				setExpiresAt(accessTokenExpiresAt!);
				setUser({}); // nothing yet

				await setRefreshTokenAsync(refreshToken);

				if (hydrateClippings) {
					const getClippingsResponse = await getClippings(
						accessToken!,
					);
					if (getClippingsResponse.success) {
						setClippings(getClippingsResponse.value);
					}
				}

				return true;
			} catch (error) {
				console.error('Failed to refresh auth session', error);
				return false;
			} finally {
				refreshInFlightRef.current = false;
			}
		},
		[setAccessToken, setClippings, setExpiresAt, setUser],
	);

	useEffect(() => {
		void refreshAuthSession(true);
	}, [refreshAuthSession]);

	useEffect(() => {
		if (!auth.accessToken || !auth.expiresAt) {
			return;
		}

		if (refreshTimerRef.current) {
			clearTimeout(refreshTimerRef.current);
		}

		const delay = Math.max(
			auth.expiresAt - Date.now() - REFRESH_LEAD_TIME_MS,
			0,
		);

		refreshTimerRef.current = setTimeout(() => {
			void refreshAuthSession(false);
		}, delay);

		return () => {
			if (refreshTimerRef.current) {
				clearTimeout(refreshTimerRef.current);
				refreshTimerRef.current = null;
			}
		};
	}, [auth.accessToken, auth.expiresAt, refreshAuthSession]);

	useEffect(() => {
		const handleAppStateChange = (nextAppState: string): void => {
			if (nextAppState !== 'active') {
				return;
			}

			if (!auth.accessToken || !auth.expiresAt) {
				return;
			}

			const refreshWindowEndsAt = auth.expiresAt - REFRESH_LEAD_TIME_MS;
			if (Date.now() >= refreshWindowEndsAt) {
				void refreshAuthSession(false);
			}
		};

		const subscription = AppState.addEventListener(
			'change',
			handleAppStateChange,
		);

		return () => subscription.remove();
	}, [auth.accessToken, auth.expiresAt, refreshAuthSession]);

	useEffect(
		() => () => {
			if (refreshTimerRef.current) {
				clearTimeout(refreshTimerRef.current);
			}
		},
		[],
	);

	useEffect(() => {
		const accessToken = auth.accessToken;

		if (!isAuthenticated || !accessToken) {
			return;
		}

		const connection = createClippingsHubConnection(() => accessToken);
		let cancelled = false;
		let retryTimer: ReturnType<typeof setTimeout> | undefined;

		const upsertClipping = (clipping: Clipping): void => {
			setClippings((prev) => {
				const next = prev.filter((item) => item.id !== clipping.id);

				return [clipping, ...next];
			});
		};

		const removeClipping = (clippingId: number): void => {
			setClippings((prev) =>
				prev.filter((item) => item.id !== clippingId),
			);
		};

		connection.on('NewClipping', (clipping) => {
			upsertClipping(clipping);
			toast.show('New clipping received');
		});
		connection.on('UpdatedClipping', upsertClipping);
		connection.on('DeletedClipping', removeClipping);

		connection.onreconnected(async () => {
			try {
				const getClippingsResponse = await getClippings(accessToken);
				if (!cancelled && getClippingsResponse.success) {
					setClippings(getClippingsResponse.value);
				}
			} catch (error) {
				console.error(
					'Failed to resync clippings after reconnect',
					error,
				);
			}
		});

		const start = async (): Promise<void> => {
			if (cancelled) {
				return;
			}

			try {
				await connection.start();
				console.log('SignalR connected to clippingsHub');
			} catch (error) {
				console.error('SignalR start failed', error);

				if (!cancelled) {
					retryTimer = setTimeout(start, 5000);
				}
			}
		};

		void start();

		return () => {
			cancelled = true;

			if (retryTimer) {
				clearTimeout(retryTimer);
			}

			connection.off('NewClipping', upsertClipping);
			connection.off('UpdatedClipping', upsertClipping);
			connection.off('DeletedClipping', removeClipping);
			void connection.stop();
		};
	}, [auth.accessToken, isAuthenticated, setClippings, toast]);

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
