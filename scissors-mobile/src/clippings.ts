/*
 * CODEX-MODIFIED: the contents of this file were written by a human and modified after the fact by a Codex agent.
 */

import { Clipping, ServerClipping } from './api/models';

export function upsertClipping(
	clippings: Clipping[],
	clipping: ServerClipping,
): Clipping[] {
	const replacedIndex = clippings.findIndex(
		(item) => item.hasServerId && item.id === clipping.id,
	);

	if (replacedIndex >= 0) {
		const next = [...clippings];
		next[replacedIndex] = clipping;
		return next;
	}

	return [clipping, ...clippings];
}
