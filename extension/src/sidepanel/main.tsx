import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import SidePanel from './SidePanel';

// const container = document.createElement('div');
// container.id = 'crxjs-app';
// document.body.appendChild(container);

createRoot(document.getElementById('root')!).render(
	<StrictMode>
		<SidePanel />
	</StrictMode>,
);
