import { emptyRequestDraft, type RequestDraftAction, type RequestDraftState } from "./types";

function normalizeQuantity(quantity: number): number {
  if (!Number.isFinite(quantity)) {
    return 1;
  }

  return Math.max(1, Math.floor(quantity));
}

export function requestDraftReducer(
  state: RequestDraftState,
  action: RequestDraftAction,
): RequestDraftState {
  switch (action.type) {
    case "hydrate":
      return action.state;
    case "addProduct": {
      const existing = state.items.find((item) => item.productId === action.product.productId);

      if (existing) {
        return {
          ...state,
          items: state.items.map((item) =>
            item.productId === action.product.productId
              ? { ...item, quantity: normalizeQuantity(item.quantity + 1) }
              : item,
          ),
        };
      }

      return {
        ...state,
        items: [
          ...state.items,
          {
            ...action.product,
            quantity: 1,
            customerComment: "",
          },
        ],
      };
    }
    case "setQuantity":
      return {
        ...state,
        items: state.items.map((item) =>
          item.productId === action.productId ? { ...item, quantity: normalizeQuantity(action.quantity) } : item,
        ),
      };
    case "setItemComment":
      return {
        ...state,
        items: state.items.map((item) =>
          item.productId === action.productId ? { ...item, customerComment: action.customerComment } : item,
        ),
      };
    case "setCustomerComment":
      return { ...state, customerComment: action.customerComment };
    case "removeItem":
      return { ...state, items: state.items.filter((item) => item.productId !== action.productId) };
    case "clear":
      return emptyRequestDraft;
    default:
      return state;
  }
}
