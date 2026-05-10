import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AdminRequestDetail } from "./admin-request-detail";
import type { AdminRequestDetail as AdminRequestDetailModel } from "@/lib/api/admin-requests";

const request: AdminRequestDetailModel = {
  number: "ЗК26-0001",
  status: { code: "new", label: "Новая" },
  source: "cart",
  itemsCount: 1,
  customer: {
    name: "Иван Петров",
    email: "ivan@example.com",
    phone: "+79000000000",
  },
  organization: {
    name: "ООО Кабельные системы",
    inn: "7700000000",
    contactPerson: "Анна Соколова",
  },
  customerComment: "Нужна поставка партиями",
  internalComment: "Уточнить наличие на складе",
  createdAt: "2026-05-07T12:30:00+03:00",
  updatedAt: "2026-05-08T09:20:00+03:00",
  items: [
    {
      productId: "11111111-1111-1111-1111-111111111111",
      productName: "Кабель U/UTP Cat 5e",
      productSku: "LC-UTP5E",
      saleUnit: { code: "coil", label: "бухта" },
      unitQuantity: "305 м",
      quantity: 2,
      customerComment: "Согласовать цвет",
    },
  ],
  history: [
    {
      event: "created",
      message: "Заявка создана.",
      createdAt: "2026-05-07T12:30:00+03:00",
    },
    {
      event: "status_changed",
      message: "Статус изменен на Новая.",
      createdAt: "2026-05-08T09:20:00+03:00",
    },
  ],
};

describe("AdminRequestDetail", () => {
  it("renders snapshots, items, history, controls, and no price wording", () => {
    render(
      <AdminRequestDetail
        request={request}
        onStatusSave={vi.fn()}
        onInternalCommentSave={vi.fn()}
        isStatusSaving={false}
        isCommentSaving={false}
        canSave={true}
        actionMessage={null}
      />,
    );

    expect(screen.getByRole("link", { name: "Вернуться к списку заявок" })).toHaveAttribute(
      "href",
      "/admin/requests",
    );
    expect(screen.getByRole("heading", { name: "Заявка ЗК26-0001" })).toBeInTheDocument();
    expect(screen.getAllByText("Новая").length).toBeGreaterThan(0);
    expect(screen.getByText("Иван Петров")).toBeInTheDocument();
    expect(screen.getByText("ivan@example.com")).toBeInTheDocument();
    expect(screen.getByText("ООО Кабельные системы")).toBeInTheDocument();
    expect(screen.getByText("7700000000")).toBeInTheDocument();
    expect(screen.getByText("Нужна поставка партиями")).toBeInTheDocument();
    expect(screen.getByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    expect(screen.getByText("LC-UTP5E")).toBeInTheDocument();
    expect(screen.getByText("2 бухта")).toBeInTheDocument();
    expect(screen.getByText("Согласовать цвет")).toBeInTheDocument();
    expect(screen.getByText("Заявка создана.")).toBeInTheDocument();
    expect(screen.getByLabelText("Статус")).toHaveValue("new");
    expect(screen.getByLabelText("Внутренний комментарий")).toHaveValue("Уточнить наличие на складе");

    const statusValues = Array.from(screen.getByLabelText("Статус").querySelectorAll("option")).map(
      (option) => option.value,
    );
    expect(statusValues).toEqual(["new", "in_progress", "completed", "cancelled"]);
    expect(statusValues).not.toContain("quoted");
    expect(statusValues).not.toContain("canceled");

    expect(screen.queryByText(/цен|оплат|счет|счёт|отгруз|кп|коммерческ|заказ/i)).not.toBeInTheDocument();
  });

  it("saves status and internal comment separately", async () => {
    const user = userEvent.setup();
    const onStatusSave = vi.fn();
    const onInternalCommentSave = vi.fn();

    render(
      <AdminRequestDetail
        request={request}
        onStatusSave={onStatusSave}
        onInternalCommentSave={onInternalCommentSave}
        isStatusSaving={false}
        isCommentSaving={false}
        canSave={true}
        actionMessage={null}
      />,
    );

    await user.selectOptions(screen.getByLabelText("Статус"), "in_progress");
    await user.click(screen.getByRole("button", { name: "Сохранить статус" }));

    expect(onStatusSave).toHaveBeenCalledWith("in_progress");
    expect(onInternalCommentSave).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText("Внутренний комментарий"));
    await user.type(screen.getByLabelText("Внутренний комментарий"), "Связаться с клиентом");
    await user.click(screen.getByRole("button", { name: "Сохранить комментарий" }));

    expect(onInternalCommentSave).toHaveBeenCalledWith("Связаться с клиентом");
    expect(onStatusSave).toHaveBeenCalledTimes(1);
  });

  it("disables save actions when session token is unavailable", () => {
    render(
      <AdminRequestDetail
        request={request}
        onStatusSave={vi.fn()}
        onInternalCommentSave={vi.fn()}
        isStatusSaving={false}
        isCommentSaving={false}
        canSave={false}
        actionMessage={null}
      />,
    );

    expect(screen.getByText("Сессия не подтверждена. Обновите страницу и войдите снова.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Сохранить статус" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Сохранить комментарий" })).toBeDisabled();
  });
});
