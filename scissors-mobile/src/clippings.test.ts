/*
 * CODEX-MODIFIED: the contents of this file were written by a human and modified after the fact by a Codex agent.
 */

import { describe, expect, it } from 'vitest';
import { upsertClipping } from './clippings';
import { Clipping, ServerClipping } from './api/models';

const temporaryClipping: Clipping = {
	temporaryId: 'temporary-1',
	text: 'local clipping',
	capturedAt: new Date('2026-08-30T12:00:00.000Z'),
	hasServerId: false,
};

const serverClipping: ServerClipping = {
	id: 42,
	text: 'server clipping',
	capturedAt: new Date('2026-08-30T13:00:00.000Z'),
	hasServerId: true,
};

describe('upsertClipping', () => {
	it('replaces a server clipping with the same id without mutating the input', () => {
		const existing: ServerClipping = {
			...serverClipping,
			text: 'old text',
		};
		const clippings = [temporaryClipping, existing];

		const result = upsertClipping(clippings, serverClipping);

		expect(result).toEqual([temporaryClipping, serverClipping]);
		expect(result).not.toBe(clippings);
		expect(clippings).toEqual([temporaryClipping, existing]);
	});

	it('prepends a new server clipping and preserves temporary clippings', () => {
		const clippings = [temporaryClipping];

		const result = upsertClipping(clippings, serverClipping);

		expect(result).toEqual([serverClipping, temporaryClipping]);
	});

	it('does not create a duplicate when the websocket event repeats', () => {
		const once = upsertClipping([], serverClipping);

		const result = upsertClipping(once, serverClipping);

		expect(result).toEqual([serverClipping]);
	});
});
