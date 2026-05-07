import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RequestDraftProvider } from "./request-draft-provider";
import { RequestDraftView } from "./request-draft-view";
import type { RequestDraftState } from "@/lib/request-draft/types";

const productDraft: RequestDraftState = {
  customerComment: "",
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

const forbiddenOrderText = ["Оформить", "заказ"].join(" ");

function renderDraftView(onSubmit = vi.fn()) {
  render(
    <RequestDraftProvider>
      <RequestDraftView onSubmit={onSubmit} />
    </RequestDraftProvider>,
  );
}

describe("RequestDraftView", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("shows empty state when there are no products", () => {
    renderDraftView();

    expect(screen.getByText("В заявке пока нет товаров")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Отправить заявку" })).toBeDisabled();
    expect(screen.queryByText(forbiddenOrderText)).not.toBeInTheDocument();
  });

  it("allows item quantity changes", async () => {
    const user = userEvent.setup();
    localStorage.setItem("linecom.requestDraft.v1", JSON.stringify(productDraft));

    renderDraftView();

    const quantityInput = await screen.findByLabelText("Количество для Кабель U/UTP Cat 5e");
    await user.clear(quantityInput);
    await user.type(quantityInput, "4");

    await waitFor(() => expect(quantityInput).toHaveValue(4));
  });

  it("removes an item", async () => {
    const user = userEvent.setup();
    localStorage.setItem("linecom.requestDraft.v1", JSON.stringify(productDraft));

    renderDraftView();

    expect(await screen.findByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Удалить Кабель U/UTP Cat 5e" }));

    expect(screen.queryByText("Кабель U/UTP Cat 5e")).not.toBeInTheDocument();
    expect(screen.getByText("В заявке пока нет товаров")).toBeInTheDocument();
  });

  it("uses request submission wording", async () => {
    localStorage.setItem("linecom.requestDraft.v1", JSON.stringify(productDraft));

    renderDraftView();

    expect(await screen.findByRole("button", { name: "Отправить заявку" })).toBeEnabled();
    expect(screen.queryByText(forbiddenOrderText)).not.toBeInTheDocument();
  });
});
