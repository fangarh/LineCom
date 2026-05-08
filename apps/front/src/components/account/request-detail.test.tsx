import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RequestDetail } from "./request-detail";
import type { CustomerRequestDetail } from "@/lib/api/requests";

const request: CustomerRequestDetail = {
  number: "ЗК26-0001",
  status: { code: "new", label: "Новая" },
  source: "cart",
  customerComment: "Нужна поставка партиями",
  createdAt: "2026-05-07T12:30:00+03:00",
  customer: {
    name: "Иван Петров",
    email: "ivan@example.com",
    phone: "+79000000000",
  },
  organization: {
    name: "ООО Кабельные системы",
    inn: "7700000000",
    contactPerson: "Иван Петров",
  },
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
  ],
};

describe("RequestDetail", () => {
  it("renders request snapshots, items, and history without prices", () => {
    render(<RequestDetail request={request} />);

    expect(screen.getByRole("heading", { name: "Заявка ЗК26-0001" })).toBeInTheDocument();
    expect(screen.getByText("Новая")).toBeInTheDocument();
    expect(screen.getAllByText("Иван Петров")).toHaveLength(2);
    expect(screen.getByText("ООО Кабельные системы")).toBeInTheDocument();
    expect(screen.getByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    expect(screen.getByText("LC-UTP5E")).toBeInTheDocument();
    expect(screen.getByText("2 бухта")).toBeInTheDocument();
    expect(screen.getByText("Согласовать цвет")).toBeInTheDocument();
    expect(screen.getByText("Заявка создана.")).toBeInTheDocument();
    expect(screen.queryByText(/цена/i)).not.toBeInTheDocument();
  });

  it("handles missing optional snapshots", () => {
    render(<RequestDetail request={{ ...request, organization: null, history: null }} />);

    expect(screen.getByText("Организация не указана")).toBeInTheDocument();
    expect(screen.getByText("История пока содержит только создание заявки.")).toBeInTheDocument();
  });
});
