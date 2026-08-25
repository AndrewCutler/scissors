import { Alert, ScrollView, StyleSheet, Text, View } from 'react-native';

import { ActionButton } from '../components/ActionButton';
import { FeatureCard } from '../components/FeatureCard';
import { theme } from '../theme';
import { GoogleSignin } from '@react-native-google-signin/google-signin';

const YOUR_WEB_CLIENT_ID = '';

GoogleSignin.configure({
	webClientId: YOUR_WEB_CLIENT_ID,
});

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
	const handleContinueWithGoogle = async (): Promise<void> => {
		const result = await GoogleSignin.signIn();

		const tokens = await GoogleSignin.getTokens();

		const idToken = tokens.idToken;
	};

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
					<ActionButton
						label="Continue with Google"
						onPress={() =>
							Alert.alert(
								'Template',
								'Wire this button to your auth flow.',
							)
						}
					/>
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
		backgroundColor: 'rgba(106, 168, 255, 0.14)',
		borderWidth: 1,
		borderColor: 'rgba(106, 168, 255, 0.28)',
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
