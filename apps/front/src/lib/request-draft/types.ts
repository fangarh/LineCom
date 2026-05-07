import type { PublicCodeLabel } from "../api/catalog";

export type RequestDraftProduct = {
  productId: string;
  slug: string;
  productName: string;
  productSku: string | null;
  saleUnit: PublicCodeLabel;
  unitQuantity: string;
};

export type RequestDraftItem = RequestDraftProduct & {
  quantity: number;
  customerComment: string;
};

export type RequestDraftState = {
  items: RequestDraftItem[];
  customerComment: string;
};

export type RequestDraftAction =
  | { type: "hydrate"; state: RequestDraftState }
  | { type: "addProduct"; product: RequestDraftProduct }
  | { type: "setQuantity"; productId: string; quantity: number }
  | { type: "setItemComment"; productId: string; customerComment: string }
  | { type: "setCustomerComment"; customerComment: string }
  | { type: "removeItem"; productId: string }
  | { type: "clear" };

export const emptyRequestDraft: RequestDraftState = {
  items: [],
  customerComment: "",
};
