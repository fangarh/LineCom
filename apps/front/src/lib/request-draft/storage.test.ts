import { beforeEach, describe, expect, it } from "vitest";
import { loadRequestDraft, saveRequestDraft } from "./storage";
import type { RequestDraftState } from "./types";

describe("request draft storage", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("loads empty draft when localStorage is empty", () => {
    expect(loadRequestDraft()).toEqual({ items: [], customerComment: "" });
  });

  it("round-trips draft state", () => {
    const state: RequestDraftState = {
      customerComment: "Позвоните перед счетом",
      items: [
        {
          productId: "11111111-1111-1111-1111-111111111111",
          slug: "u-utp-cat-5e",
          productName: "Кабель U/UTP Cat 5e",
          productSku: "LC-UTP5E",
          saleUnit: { code: "coil", label: "бухта" },
          unitQuantity: "305 м",
          quantity: 2,
          customerComment: "",
        },
      ],
    };

    saveRequestDraft(state);

    expect(loadRequestDraft()).toEqual(state);
  });

  it("falls back to empty draft for invalid persisted payloads", () => {
    localStorage.setItem("linecom.requestDraft.v1", "{not json");
    expect(loadRequestDraft()).toEqual({ items: [], customerComment: "" });

    localStorage.setItem("linecom.requestDraft.v1", JSON.stringify({ items: "bad" }));
    expect(loadRequestDraft()).toEqual({ items: [], customerComment: "" });
  });
});
