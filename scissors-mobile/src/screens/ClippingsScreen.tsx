import { useContext } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { Clipping } from 'src/api/models';
import { AppContext } from 'src/context/AppContext';

export function ClippingsScreen() {
	const { clippings } = useContext(AppContext);

	return (
		<ScrollView
			contentContainerStyle={styles.content}
			showsVerticalScrollIndicator
		>
			<View>
				<View>Clippings</View>
				{clippings.map((c) => {
					return <View>test</View>;
				})}
			</View>
		</ScrollView>
	);
}

const styles = StyleSheet.create({
	content: {},
});
