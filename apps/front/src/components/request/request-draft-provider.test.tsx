"use client";

import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RequestDraftProvider, useRequestDraft } from "./request-draft-provider";

function DraftCountProbe() {
  const { state } = useRequestDraft();

  return <output aria-label="Позиций в заявке">{state.items.length}</output>;
}

describe("RequestDraftProvider", () => {
  it("hydrates draft from localStorage", async () => {
    localStorage.setItem(
      "linecom.requestDraft.v1",
      JSON.stringify({
        customerComment: "",
        items: [
          {
            productId: "11111111-1111-1111-1111-111111111111",
            slug: "u-utp-cat-5e",
            productName: "Кабель U/UTP Cat 5e",
            productSku: "LC-UTP5E",
            saleUnit: { code: "coil", label: "бухта" },
            unitQuantity: "305 м",
            quantity: 1,
            customerComment: "",
          },
        ],
      }),
    );

    render(
      <RequestDraftProvider>
        <DraftCountProbe />
      </RequestDraftProvider>,
    );

    await waitFor(() => expect(screen.getByLabelText("Позиций в заявке")).toHaveTextContent("1"));
  });
});
