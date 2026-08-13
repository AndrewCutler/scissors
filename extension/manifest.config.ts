import { defineManifest } from '@crxjs/vite-plugin';
import pkg from './package.json';

export default defineManifest({
	manifest_version: 3,
	name: pkg.name,
	version: pkg.version,
	icons: {
		48: 'public/logo.png',
	},
	action: {
		default_icon: {
			48: 'public/logo.png',
		},
	},
	commands: {
		sync: {
			suggested_key: { default: 'Alt+C', mac: 'Alt+C' },
			description: 'Sync clipboard',
		},
	},
	permissions: ['sidePanel', 'contentSettings'],
	side_panel: {
		default_path: 'src/sidepanel/index.html',
	},
	background: {
		service_worker: 'src/service-workers/serviceWorker.ts',
		type: 'module',
	},
});
