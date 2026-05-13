import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { RequestList } from "./request-list";
import type { CustomerRequestListItem } from "@/lib/api/requests";

const requests: CustomerRequestListItem[] = [
  {
    number: "ЗК26-0001",
    status: { code: "new", label: "Новая" },
    source: "cart",
    itemsCount: 2,
    customerComment: "Нужна поставка партиями",
    createdAt: "2026-05-07T12:30:00+03:00",
  },
  {
    number: "ЗК26-0002",
    status: { code: "completed", label: "Завершена" },
    source: "cart",
    itemsCount: 1,
    customerComment: null,
    createdAt: "2026-05-07T15:45:00+03:00",
  },
];

describe("RequestList", () => {
  it("renders request summaries with public numbers and detail links", () => {
    render(<RequestList requests={requests} status="all" onStatusChange={vi.fn()} onPreviewRequest={vi.fn()} />);

    expect(screen.getByRole("heading", { name: "ЗК26-0001" })).toBeInTheDocument();
    expect(screen.getByText("Новая")).toBeInTheDocument();
    expect(screen.getByText("2 позиции")).toBeInTheDocument();
    expect(screen.getByText("Нужна поставка партиями")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Открыть заявку ЗК26-0001" })).toHaveAttribute(
      "href",
      "/account/requests/%D0%97%D0%9A26-0001",
    );
    expect(screen.queryByText(/цена/i)).not.toBeInTheDocument();
  });

  it("notifies when status filter changes", async () => {
    const user = userEvent.setup();
    const onStatusChange = vi.fn();

    render(<RequestList requests={requests} status="all" onStatusChange={onStatusChange} onPreviewRequest={vi.fn()} />);

    await user.selectOptions(screen.getByLabelText("Статус заявок"), "completed");

    expect(onStatusChange).toHaveBeenCalledWith("completed");
  });

  it("notifies when quick preview is requested", async () => {
    const user = userEvent.setup();
    const onPreviewRequest = vi.fn();

    render(
      <RequestList
        requests={requests}
        status="all"
        onStatusChange={vi.fn()}
        onPreviewRequest={onPreviewRequest}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Быстрый просмотр ЗК26-0001" }));

    expect(onPreviewRequest).toHaveBeenCalledWith("ЗК26-0001");
  });

  it("uses the release cancellation status code", async () => {
    const user = userEvent.setup();
    const onStatusChange = vi.fn();

    render(<RequestList requests={requests} status="all" onStatusChange={onStatusChange} onPreviewRequest={vi.fn()} />);

    await user.selectOptions(screen.getByRole("combobox"), "cancelled");

    expect(onStatusChange).toHaveBeenCalledWith("cancelled");
  });

  it("shows an empty state without order wording", () => {
    const forbiddenOrderText = ["Оформить", "заказ"].join(" ");

    render(<RequestList requests={[]} status="all" onStatusChange={vi.fn()} onPreviewRequest={vi.fn()} />);

    expect(screen.getByText("У вас пока нет заявок")).toBeInTheDocument();
    expect(screen.queryByText(forbiddenOrderText)).not.toBeInTheDocument();
  });
});
