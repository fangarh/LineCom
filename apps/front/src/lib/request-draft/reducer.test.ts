import { describe, expect, it } from "vitest";
import { getDraftItemsCount, isDraftEmpty } from "./selectors";
import { requestDraftReducer } from "./reducer";
import type { RequestDraftState } from "./types";

const empty: RequestDraftState = { items: [], customerComment: "" };

const product = {
  productId: "11111111-1111-1111-1111-111111111111",
  slug: "u-utp-cat-5e",
  productName: "Кабель U/UTP Cat 5e",
  productSku: "LC-UTP5E",
  saleUnit: { code: "coil", label: "бухта" },
  unitQuantity: "305 м",
};

describe("requestDraftReducer", () => {
  it("adds product as one sale unit", () => {
    const state = requestDraftReducer(empty, { type: "addProduct", product });

    expect(state.items).toHaveLength(1);
    expect(state.items[0].quantity).toBe(1);
  });

  it("increments existing product instead of duplicating it", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const two = requestDraftReducer(one, { type: "addProduct", product });

    expect(two.items).toHaveLength(1);
    expect(two.items[0].quantity).toBe(2);
  });

  it("does not allow quantity below one", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const updated = requestDraftReducer(one, {
      type: "setQuantity",
      productId: product.productId,
      quantity: 0,
    });

    expect(updated.items[0].quantity).toBe(1);
  });

  it("floors fractional quantity to whole sale units", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const updated = requestDraftReducer(one, {
      type: "setQuantity",
      productId: product.productId,
      quantity: 2.7,
    });

    expect(updated.items[0].quantity).toBe(2);
  });

  it("updates item and customer comments", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const withItemComment = requestDraftReducer(one, {
      type: "setItemComment",
      productId: product.productId,
      customerComment: "Нужен аналог, если этой бухты нет",
    });
    const withCustomerComment = requestDraftReducer(withItemComment, {
      type: "setCustomerComment",
      customerComment: "Позвоните перед счетом",
    });

    expect(withCustomerComment.items[0].customerComment).toBe("Нужен аналог, если этой бухты нет");
    expect(withCustomerComment.customerComment).toBe("Позвоните перед счетом");
  });

  it("removes product and clears draft", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const removed = requestDraftReducer(one, { type: "removeItem", productId: product.productId });
    const withCustomerComment = requestDraftReducer(one, {
      type: "setCustomerComment",
      customerComment: "Позвоните перед счетом",
    });
    const cleared = requestDraftReducer(withCustomerComment, { type: "clear" });

    expect(removed.items).toEqual([]);
    expect(cleared).toEqual(empty);
  });

  it("hydrates from stored state and exposes selectors", () => {
    const stored: RequestDraftState = {
      customerComment: "Позвоните перед счетом",
      items: [
        { ...product, quantity: 3, customerComment: "" },
        {
          ...product,
          productId: "22222222-2222-2222-2222-222222222222",
          slug: "patch-cord",
          productName: "Патч-корд",
          productSku: null,
          quantity: 2,
          customerComment: "Согласовать цвет",
        },
      ],
    };

    const state = requestDraftReducer(empty, { type: "hydrate", state: stored });

    expect(state).toEqual(stored);
    expect(getDraftItemsCount(state)).toBe(5);
    expect(isDraftEmpty(state)).toBe(false);
    expect(isDraftEmpty(empty)).toBe(true);
  });
});
