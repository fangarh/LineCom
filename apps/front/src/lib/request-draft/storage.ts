import { emptyRequestDraft, type RequestDraftItem, type RequestDraftState } from "./types";

const STORAGE_KEY = "linecom.requestDraft.v1";

function isStringRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object";
}

function isDraftItem(value: unknown): value is RequestDraftItem {
  if (!isStringRecord(value) || !isStringRecord(value.saleUnit)) {
    return false;
  }

  return (
    typeof value.productId === "string" &&
    typeof value.slug === "string" &&
    typeof value.productName === "string" &&
    (typeof value.productSku === "string" || value.productSku === null) &&
    typeof value.saleUnit.code === "string" &&
    typeof value.saleUnit.label === "string" &&
    typeof value.unitQuantity === "string" &&
    typeof value.quantity === "number" &&
    Number.isFinite(value.quantity) &&
    value.quantity >= 1 &&
    typeof value.customerComment === "string"
  );
}

function parseRequestDraft(value: unknown): RequestDraftState {
  if (!isStringRecord(value) || !Array.isArray(value.items)) {
    return emptyRequestDraft;
  }

  if (!value.items.every(isDraftItem)) {
    return emptyRequestDraft;
  }

  return {
    items: value.items,
    customerComment: typeof value.customerComment === "string" ? value.customerComment : "",
  };
}

export function loadRequestDraft(): RequestDraftState {
  if (typeof window === "undefined") {
    return emptyRequestDraft;
  }

  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return emptyRequestDraft;
  }

  try {
    return parseRequestDraft(JSON.parse(raw));
  } catch {
    return emptyRequestDraft;
  }
}

export function saveRequestDraft(state: RequestDraftState): void {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}
