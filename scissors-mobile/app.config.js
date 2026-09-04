/*
 * CODEX-GENERATED: the contents of this file were fully constructed by a Codex agent and not a human.
 */

const baseConfig = require('./app.json');

module.exports = ({ config }) => ({
	...baseConfig.expo,
	...config,
	android: {
		...baseConfig.expo.android,
		...config.android,
		// Local phone testing uses an HTTP API on the development machine.
		usesCleartextTraffic: process.env.EXPO_PUBLIC_ALLOW_HTTP === 'true',
	},
});
