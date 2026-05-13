import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { AdminRequestListItem } from "@/lib/api/admin-requests";
import { AdminRequestList } from "./admin-request-list";

const requests: AdminRequestListItem[] = [
  {
    number: "ЗК26-0001",
    status: { code: "new", label: "Новая" },
    source: "cart",
    itemsCount: 3,
    customer: {
      name: "Иван Петров",
      email: "ivan@example.com",
      phone: "+7 900 111-22-33",
    },
    organization: {
      name: "ООО Вектор",
      inn: "7701000000",
      contactPerson: "Мария",
    },
    customerComment: "Нужна поставка партиями и проверка наличия.",
    internalComment: "Клиент просил связаться утром.",
    createdAt: "2026-05-07T12:30:00+03:00",
    updatedAt: "2026-05-07T13:00:00+03:00",
  },
];

describe("AdminRequestList", () => {
  it("renders filters, request summary, and detail link", () => {
    render(
      <AdminRequestList
        filters={{ status: "all", number: "", contact: "", organization: "" }}
        requests={requests}
        onFiltersChange={vi.fn()}
        onPreviewRequest={vi.fn()}
      />,
    );

    expect(screen.getByRole("heading", { name: "Заявки" })).toBeInTheDocument();
    expect(screen.getByLabelText("Статус")).toBeInTheDocument();
    expect(screen.getByLabelText("Номер")).toBeInTheDocument();
    expect(screen.getByLabelText("Контакт")).toBeInTheDocument();
    expect(screen.getByLabelText("Организация")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "ЗК26-0001" })).toBeInTheDocument();
    expect(screen.getByText("Новая")).toBeInTheDocument();
    expect(screen.getByText("Иван Петров")).toBeInTheDocument();
    expect(screen.getByText("ООО Вектор")).toBeInTheDocument();
    expect(screen.getByText("3 позиции")).toBeInTheDocument();
    expect(screen.getByText("Черновик заявки")).toBeInTheDocument();
    expect(screen.getByText("Нужна поставка партиями и проверка наличия.")).toBeInTheDocument();
    expect(screen.getByText("Клиент просил связаться утром.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Открыть заявку ЗК26-0001" })).toHaveAttribute(
      "href",
      "/admin/requests/%D0%97%D0%9A26-0001",
    );
    expect(screen.getByRole("button", { name: "Быстрый просмотр ЗК26-0001" })).toBeInTheDocument();
    expect(screen.queryByRole("option", { name: /quoted/i })).not.toBeInTheDocument();
  });

  it("calls onPreviewRequest from the quick preview button", async () => {
    const user = userEvent.setup();
    const onPreviewRequest = vi.fn();

    render(
      <AdminRequestList
        filters={{ status: "all", number: "", contact: "", organization: "" }}
        requests={requests}
        onFiltersChange={vi.fn()}
        onPreviewRequest={onPreviewRequest}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Быстрый просмотр ЗК26-0001" }));

    expect(onPreviewRequest).toHaveBeenCalledWith("ЗК26-0001");
  });

  it("emits filter changes", () => {
    const onFiltersChange = vi.fn();

    render(
      <AdminRequestList
        filters={{ status: "all", number: "", contact: "", organization: "" }}
        requests={requests}
        onFiltersChange={onFiltersChange}
        onPreviewRequest={vi.fn()}
      />,
    );

    fireEvent.change(screen.getByLabelText("Статус"), { target: { value: "in_progress" } });
    fireEvent.change(screen.getByLabelText("Номер"), { target: { value: "0001" } });
    fireEvent.change(screen.getByLabelText("Контакт"), { target: { value: "ivan" } });
    fireEvent.change(screen.getByLabelText("Организация"), { target: { value: "Вектор" } });

    expect(onFiltersChange).toHaveBeenCalledWith({ status: "in_progress", number: "", contact: "", organization: "" });
    expect(onFiltersChange).toHaveBeenCalledWith({ status: "all", number: "0001", contact: "", organization: "" });
    expect(onFiltersChange).toHaveBeenCalledWith({ status: "all", number: "", contact: "ivan", organization: "" });
    expect(onFiltersChange).toHaveBeenCalledWith({ status: "all", number: "", contact: "", organization: "Вектор" });
  });

  it("shows an empty state", () => {
    render(
      <AdminRequestList
        filters={{ status: "all", number: "", contact: "", organization: "" }}
        requests={[]}
        onFiltersChange={vi.fn()}
        onPreviewRequest={vi.fn()}
      />,
    );

    expect(screen.getByText("Заявки не найдены")).toBeInTheDocument();
  });
});
