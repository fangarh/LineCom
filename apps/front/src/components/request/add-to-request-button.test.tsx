import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { RequestDraftProvider } from "./request-draft-provider";
import { AddToRequestButton } from "./add-to-request-button";

describe("AddToRequestButton", () => {
  it("adds the product to the request draft", async () => {
    const user = userEvent.setup();

    render(
      <RequestDraftProvider>
        <AddToRequestButton
          product={{
            productId: "11111111-1111-1111-1111-111111111111",
            slug: "u-utp-cat-5e",
            productName: "Кабель U/UTP Cat 5e",
            productSku: "LC-UTP5E",
            saleUnit: { code: "coil", label: "бухта" },
            unitQuantity: "305 м",
          }}
        />
      </RequestDraftProvider>,
    );

    await user.click(screen.getByRole("button", { name: "Добавить в заявку" }));

    await waitFor(() => {
      const stored = JSON.parse(localStorage.getItem("linecom.requestDraft.v1") ?? "{}") as {
        items?: Array<{ productId: string; quantity: number }>;
      };

      expect(stored.items).toHaveLength(1);
      expect(stored.items?.[0]).toMatchObject({
        productId: "11111111-1111-1111-1111-111111111111",
        productName: "Кабель U/UTP Cat 5e",
        quantity: 1,
      });
    });
  });
});
