import type { RequestDraftState } from "./types";

export function getDraftItemsCount(state: RequestDraftState): number {
  return state.items.reduce((sum, item) => sum + item.quantity, 0);
}

export function isDraftEmpty(state: RequestDraftState): boolean {
  return state.items.length === 0;
}
