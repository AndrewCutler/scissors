import { Pressable, StyleSheet, Text, ViewStyle } from 'react-native';

import { theme } from '../theme';

type ActionButtonProps = {
	label: string;
	onPress: () => void;
	variant?: 'primary' | 'secondary';
	style?: ViewStyle;
};

export function ActionButton({
	label,
	onPress,
	variant = 'primary',
	style,
}: ActionButtonProps) {
	return (
		<Pressable
			accessibilityRole="button"
			onPress={onPress}
			style={({ pressed }) => [
				styles.button,
				variant === 'primary' ? styles.primary : styles.secondary,
				pressed && styles.pressed,
				style,
			]}
		>
			<Text
				style={[
					styles.label,
					variant === 'secondary' && styles.secondaryLabel,
				]}
			>
				{label}
			</Text>
		</Pressable>
	);
}

const styles = StyleSheet.create({
	button: {
		minHeight: 48,
		paddingHorizontal: theme.spacing.lg,
		borderRadius: theme.radius.pill,
		alignItems: 'center',
		justifyContent: 'center',
		borderWidth: 1,
	},
	primary: {
		backgroundColor: theme.colors.primaryStrong,
		borderColor: 'rgba(255, 255, 255, 0.04)',
	},
	secondary: {
		backgroundColor: 'transparent',
		borderColor: theme.colors.border,
	},
	pressed: {
		opacity: 0.86,
		transform: [{ scale: 0.99 }],
	},
	label: {
		color: theme.colors.text,
		fontSize: 16,
		fontWeight: '700',
		letterSpacing: 0.2,
	},
	secondaryLabel: {
		color: theme.colors.text,
	},
});
