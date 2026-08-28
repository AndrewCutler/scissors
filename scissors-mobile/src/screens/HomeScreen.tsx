import {
	Alert,
	Clipboard,
	FlatList,
	Pressable,
	ScrollView,
	StyleSheet,
	Text,
	View,
} from 'react-native';

import { ActionButton } from '../components/ActionButton';
import { FeatureCard } from '../components/FeatureCard';
import { theme } from '../theme';
import { GoogleSignin } from '@react-native-google-signin/google-signin';
import { GoogleWebAuthResponseDTO } from 'src/api/models';
import { useContext, useEffect, useRef, useState } from 'react';
import { AppContext } from 'src/context/AppContext';
import { completeGoogleAuth, getClippings } from 'src/api/api';
import { setRefreshTokenAsync } from 'src/util/storage';

GoogleSignin.configure({
	webClientId: process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID,
});

const CLIPPED_TEXT_LINES = 6;

const features = [
	{
		title: 'Clipboard capture',
		description:
			'Quickly grab text from the clipboard and keep it ready to sync.',
		accent: theme.colors.primary,
	},
	{
		title: 'Server sync',
		description:
			'Mark items as synced and prepare them for backend upload workflows.',
		accent: theme.colors.success,
	},
	{
		title: 'Fast review',
		description:
			'Browse recent clippings in a focused mobile-first layout.',
		accent: theme.colors.danger,
	},
];

export function HomeScreen() {
	const {
		auth: { isAuthenticated },
		setAccessToken,
		setExpiresAt,
		setUser,
		clippings,
		setClippings,
	} = useContext(AppContext);
	const [copyMessage, setCopyMessage] = useState<string | null>(null);
	const copyMessageTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
	const [overflowingClippingIds, setOverflowingClippingIds] = useState<
		Set<string>
	>(() => new Set());
	const [expandedClippingIds, setExpandedClippingIds] = useState<Set<string>>(
		() => new Set(),
	);
	const orderedClippings = [...clippings].sort((a, b) => {
		const aTime = new Date(a.capturedAt).getTime();
		const bTime = new Date(b.capturedAt).getTime();
		return bTime - aTime;
	});

	useEffect(
		() => () => {
			if (copyMessageTimer.current) {
				clearTimeout(copyMessageTimer.current);
			}
		},
		[],
	);

	const handleCopyClipping = (text: string): void => {
		Clipboard.setString(text);
		setCopyMessage('Text copied');

		if (copyMessageTimer.current) {
			clearTimeout(copyMessageTimer.current);
		}

		copyMessageTimer.current = setTimeout(() => {
			setCopyMessage(null);
			copyMessageTimer.current = null;
		}, 1600);
	};

	const markClippingOverflowing = (
		id: string,
		isOverflowing: boolean,
	): void =>
		setOverflowingClippingIds((prev) => {
			const hasOverflow = prev.has(id);
			if (hasOverflow === isOverflowing) {
				return prev;
			}

			const next = new Set(prev);
			if (isOverflowing) {
				next.add(id);
			} else {
				next.delete(id);
			}

			return next;
		});

	const expandClipping = (id: string): void => {
		setExpandedClippingIds((prev) => {
			if (prev.has(id)) {
				return prev;
			}

			const next = new Set(prev);
			next.add(id);
			return next;
		});
	};

	const handleContinueWithGoogle = async (): Promise<void> => {
		// TODO: Web implementation.
		try {
			let idToken = '';

			const signInResponse = await GoogleSignin.signIn();

			if (!signInResponse || signInResponse.type === 'cancelled') {
				const tokens = await GoogleSignin.getTokens();
				idToken = tokens.idToken;
			} else {
				idToken = signInResponse.data?.idToken ?? '';
			}

			if (idToken) {
				const response = await completeGoogleAuth(idToken);
				if (response) {
					setExpiresAt(response.accessTokenExpiresAt);
					setAccessToken(response.accessToken);
					setUser({}); // user is nothing yet

					await setRefreshTokenAsync(response.refreshToken);

					const data = await getClippings(response.accessToken);
					setClippings(data ?? []);
				}
			} else {
				console.error('idToken not found in response.');
			}
		} catch (e) {
			console.error(e);
		}
	};

	const formatCapturedAt = (capturedAt: Date | string): string =>
		new Date(capturedAt).toLocaleString(undefined, {
			dateStyle: 'medium',
			timeStyle: 'short',
		});

	if (isAuthenticated) {
		return (
			<View style={styles.listScreen}>
				{copyMessage ? (
					<View style={styles.toast} pointerEvents="none">
						<Text style={styles.toastText}>{copyMessage}</Text>
					</View>
				) : null}
				<FlatList
					contentContainerStyle={styles.listContent}
					data={orderedClippings}
					keyExtractor={(item) => String(item.id)}
					showsVerticalScrollIndicator
					ListHeaderComponent={
						<View style={styles.listHeader}>
							<Text style={styles.listTitle}>Clippings</Text>
						</View>
					}
					ListEmptyComponent={
						<View style={styles.emptyState}>
							<Text style={styles.emptyTitle}>
								No clippings yet
							</Text>
							<Text style={styles.emptyCaption}>
								Once you capture or sync your first clipping, it
								will appear here.
							</Text>
						</View>
					}
					renderItem={({ item }) => {
						const clippingId = String(item.id);
						const isExpanded = expandedClippingIds.has(clippingId);
						const isOverflowing =
							overflowingClippingIds.has(clippingId);

						return (
							<View style={styles.clippingCard}>
								<View style={styles.clippingHeader}>
									<View style={styles.clippingHeaderLeft}>
										<View style={styles.clippingMeta}>
											<Text
												style={styles.clippingMetaLabel}
											>
												Captured at
											</Text>
											<Text
												style={styles.clippingMetaValue}
											>
												{formatCapturedAt(
													item.capturedAt,
												)}
											</Text>
										</View>
									</View>

									<View style={styles.clippingActions}>
										<Pressable
											accessibilityRole="button"
											accessibilityLabel="Copy clipping text"
											onPress={() =>
												handleCopyClipping(item.text)
											}
											style={({ pressed }) => [
												styles.iconButton,
												styles.copyButton,
												pressed &&
													styles.iconButtonPressed,
											]}
										>
											<Text style={styles.copyIcon}>
												⧉
											</Text>
										</Pressable>

										<Pressable
											accessibilityRole="button"
											accessibilityLabel="Sync clipping"
											onPress={() => undefined}
											style={({ pressed }) => [
												styles.iconButton,
												styles.syncButton,
												pressed &&
													styles.iconButtonPressed,
											]}
										>
											<Text style={styles.syncIcon}>
												↗
											</Text>
										</Pressable>

										<Pressable
											accessibilityRole="button"
											accessibilityLabel="Delete clipping"
											onPress={() => undefined}
											style={({ pressed }) => [
												styles.iconButton,
												styles.deleteButton,
												pressed &&
													styles.iconButtonPressed,
											]}
										>
											<Text style={styles.deleteIcon}>
												×
											</Text>
										</Pressable>
									</View>
								</View>

								<Pressable
									accessibilityRole={
										isOverflowing && !isExpanded
											? 'button'
											: undefined
									}
									accessibilityLabel={
										isOverflowing && !isExpanded
											? 'Expand clipping text'
											: undefined
									}
									disabled={!isOverflowing || isExpanded}
									onPress={() => expandClipping(clippingId)}
									style={styles.clippingTextPressable}
								>
									<Text
										style={[
											styles.clippingText,
											isExpanded &&
												styles.clippingTextExpanded,
										]}
										numberOfLines={
											isExpanded
												? undefined
												: CLIPPED_TEXT_LINES
										}
										ellipsizeMode="tail"
										onTextLayout={
											isExpanded
												? undefined
												: ({ nativeEvent }) =>
														markClippingOverflowing(
															clippingId,
															nativeEvent.lines
																.length >
																CLIPPED_TEXT_LINES,
														)
										}
									>
										{item.text}
									</Text>
								</Pressable>
							</View>
						);
					}}
				/>
			</View>
		);
	}

	return (
		<ScrollView
			contentContainerStyle={styles.content}
			showsVerticalScrollIndicator={false}
		>
			<View style={styles.hero}>
				<View style={styles.badge}>
					<Text style={styles.badgeText}>Scissors Mobile</Text>
				</View>
				<Text style={styles.title}>
					Clip, sync, and review on the go.
				</Text>
				<Text style={styles.subtitle}>
					A clean React Native starter for the mobile companion app.
					Use this as the foundation for auth, clipboard capture, and
					sync flows.
				</Text>

				<View style={styles.actions}>
					{!isAuthenticated && (
						<ActionButton
							variant="primary"
							label="Continue with Google"
							onPress={handleContinueWithGoogle}
						/>
					)}
					<ActionButton
						label="Capture clipboard"
						variant="secondary"
						onPress={() =>
							Alert.alert(
								'Template',
								'Hook this up to clipboard capture.',
							)
						}
					/>
				</View>
			</View>

			<View style={styles.sectionHeader}>
				<Text style={styles.sectionTitle}>Starter features</Text>
				<Text style={styles.sectionCaption}>
					Drop in your real app logic here.
				</Text>
			</View>

			<View style={styles.grid}>
				{features.map((feature) => (
					<FeatureCard
						key={feature.title}
						title={feature.title}
						description={feature.description}
						accent={feature.accent}
					/>
				))}
			</View>
		</ScrollView>
	);
}

const styles = StyleSheet.create({
	content: {
		padding: theme.spacing.lg,
		gap: theme.spacing.lg,
		alignSelf: 'center',
		maxWidth: 800,
	},
	listContent: {
		padding: theme.spacing.lg,
		paddingBottom: theme.spacing.xl,
		gap: theme.spacing.md,
	},
	listScreen: {
		flex: 1,
	},
	toast: {
		position: 'absolute',
		top: theme.spacing.lg,
		left: theme.spacing.lg,
		right: theme.spacing.lg,
		zIndex: 20,
		alignItems: 'center',
	},
	toastText: {
		paddingHorizontal: theme.spacing.md,
		paddingVertical: 10,
		borderRadius: theme.radius.pill,
		backgroundColor: 'rgba(42, 28, 23, 0.92)',
		color: '#FFF8F2',
		fontSize: 13,
		fontWeight: '700',
		letterSpacing: 0.2,
		overflow: 'hidden',
	},
	listHeader: {
		gap: 6,
		paddingBottom: theme.spacing.xs,
	},
	listTitle: {
		color: theme.colors.text,
		fontSize: 30,
		lineHeight: 36,
		fontWeight: '800',
		letterSpacing: -0.4,
	},
	emptyState: {
		marginTop: theme.spacing.lg,
		padding: theme.spacing.lg,
		borderRadius: 24,
		backgroundColor: theme.colors.surfaceStrong,
		borderWidth: 1,
		borderColor: theme.colors.border,
		gap: 8,
	},
	emptyTitle: {
		color: theme.colors.text,
		fontSize: 18,
		fontWeight: '800',
	},
	emptyCaption: {
		color: theme.colors.textMuted,
		fontSize: 14,
		lineHeight: 20,
	},
	clippingCard: {
		padding: theme.spacing.lg,
		borderRadius: 24,
		backgroundColor: theme.colors.surfaceStrong,
		borderWidth: 1,
		borderColor: theme.colors.border,
		gap: theme.spacing.md,
	},
	clippingHeader: {
		flexDirection: 'row',
		alignItems: 'flex-start',
		justifyContent: 'space-between',
		gap: theme.spacing.md,
	},
	clippingHeaderLeft: {
		flexDirection: 'row',
		alignItems: 'center',
		flex: 1,
		gap: theme.spacing.md,
		minWidth: 0,
	},
	clippingMeta: {
		flex: 1,
		minWidth: 0,
	},
	clippingMetaLabel: {
		color: theme.colors.textMuted,
		fontSize: 12,
		fontWeight: '700',
		letterSpacing: 0.6,
		textTransform: 'uppercase',
	},
	clippingMetaValue: {
		color: theme.colors.text,
		fontSize: 15,
		fontWeight: '700',
		lineHeight: 20,
	},
	clippingActions: {
		flexDirection: 'row',
		alignItems: 'center',
		gap: 8,
	},
	iconButton: {
		width: 34,
		height: 34,
		borderRadius: 17,
		alignItems: 'center',
		justifyContent: 'center',
		borderWidth: 1,
	},
	iconButtonPressed: {
		opacity: 0.84,
		transform: [{ scale: 0.98 }],
	},
	copyButton: {
		backgroundColor: 'rgba(93, 107, 124, 0.14)',
		borderColor: 'rgba(93, 107, 124, 0.26)',
	},
	syncButton: {
		backgroundColor: 'rgba(78, 211, 138, 0.12)',
		borderColor: 'rgba(78, 211, 138, 0.25)',
	},
	deleteButton: {
		backgroundColor: 'rgba(255, 114, 114, 0.12)',
		borderColor: 'rgba(255, 114, 114, 0.24)',
	},
	copyIcon: {
		color: theme.colors.textMuted,
		fontSize: 17,
		fontWeight: '800',
		lineHeight: 17,
	},
	syncIcon: {
		color: theme.colors.success,
		fontSize: 17,
		fontWeight: '900',
		lineHeight: 17,
	},
	deleteIcon: {
		color: theme.colors.danger,
		fontSize: 22,
		fontWeight: '800',
		lineHeight: 22,
	},
	clippingText: {
		color: theme.colors.text,
		fontSize: 15,
		lineHeight: 22,
	},
	clippingTextPressable: {
		width: '100%',
	},
	clippingTextExpanded: {
		paddingBottom: 2,
	},
	hero: {
		gap: theme.spacing.md,
		padding: theme.spacing.lg,
		borderRadius: 28,
		backgroundColor: theme.colors.surfaceStrong,
		borderWidth: 1,
		borderColor: theme.colors.border,
	},
	badge: {
		alignSelf: 'flex-start',
		paddingHorizontal: theme.spacing.md,
		paddingVertical: 8,
		borderRadius: theme.radius.pill,
		color: theme.colors.text,
		// backgroundColor: 'rgba(106, 168, 255, 0.14)',
		borderWidth: 1,
		borderColor: theme.colors.border,
	},
	badgeText: {
		color: theme.colors.primary,
		fontSize: 12,
		fontWeight: '700',
		letterSpacing: 1.2,
		textTransform: 'uppercase',
	},
	title: {
		color: theme.colors.text,
		fontSize: 38,
		lineHeight: 44,
		fontWeight: '800',
		letterSpacing: -0.6,
	},
	subtitle: {
		color: theme.colors.textMuted,
		fontSize: 16,
		lineHeight: 24,
	},
	actions: {
		gap: theme.spacing.sm,
		marginTop: theme.spacing.xs,
	},
	sectionHeader: {
		gap: 6,
	},
	sectionTitle: {
		color: theme.colors.text,
		fontSize: 20,
		fontWeight: '800',
	},
	sectionCaption: {
		color: theme.colors.textMuted,
		fontSize: 14,
	},
	grid: {
		gap: theme.spacing.md,
	},
});
