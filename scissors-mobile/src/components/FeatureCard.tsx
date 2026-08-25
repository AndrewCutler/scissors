import { StyleSheet, Text, View } from 'react-native';

import { theme } from '../theme';

type FeatureCardProps = {
	title: string;
	description: string;
	accent: string;
};

export function FeatureCard({ title, description, accent }: FeatureCardProps) {
	return (
		<View style={styles.card}>
			<View style={[styles.accent, { backgroundColor: accent }]} />
			<Text style={styles.title}>{title}</Text>
			<Text style={styles.description}>{description}</Text>
		</View>
	);
}

const styles = StyleSheet.create({
	card: {
		borderRadius: theme.radius.lg,
		padding: theme.spacing.md,
		backgroundColor: theme.colors.surface,
		borderWidth: 1,
		borderColor: theme.colors.border,
	},
	accent: {
		width: 42,
		height: 5,
		borderRadius: theme.radius.pill,
		marginBottom: theme.spacing.md,
	},
	title: {
		color: theme.colors.text,
		fontSize: 16,
		fontWeight: '700',
		marginBottom: 6,
	},
	description: {
		color: theme.colors.textMuted,
		fontSize: 14,
		lineHeight: 20,
	},
});
