import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { ApiClientError } from "@/lib/api/errors";
import { RequestsPageClient } from "./requests-page-client";

const routerMock = vi.hoisted(() => ({
  push: vi.fn(),
}));

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
}));

const requestsApiMock = vi.hoisted(() => ({
  getCustomerRequests: vi.fn(),
  getCustomerRequest: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => routerMock,
}));

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();
  return {
    ...actual,
    getMe: authApiMock.getMe,
  };
});

vi.mock("@/lib/api/requests", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/requests")>();
  return {
    ...actual,
    getCustomerRequests: requestsApiMock.getCustomerRequests,
    getCustomerRequest: requestsApiMock.getCustomerRequest,
  };
});

function renderPage() {
  render(
    <AuthProvider>
      <RequestsPageClient />
    </AuthProvider>,
  );
}

describe("RequestsPageClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "11111111-1111-1111-1111-111111111111",
        name: "Иван",
        email: "ivan@example.com",
        phone: null,
        role: "customer",
      },
      csrfToken: "csrf",
    });
  });

  it("loads current user and renders request list", async () => {
    requestsApiMock.getCustomerRequests.mockResolvedValue({
      items: [
        {
          number: "ЗК26-0001",
          status: { code: "new", label: "Новая" },
          source: "cart",
          itemsCount: 2,
          customerComment: null,
          createdAt: "2026-05-07T12:30:00+03:00",
        },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });

    renderPage();

    expect(await screen.findByRole("heading", { name: "ЗК26-0001" })).toBeInTheDocument();
    expect(authApiMock.getMe).toHaveBeenCalledTimes(1);
    expect(requestsApiMock.getCustomerRequests).toHaveBeenCalledWith({ status: undefined });
  });

  it("opens a quick preview drawer with customer request details only", async () => {
    const user = userEvent.setup();

    requestsApiMock.getCustomerRequests.mockResolvedValue({
      items: [
        {
          number: "ЗК26-0001",
          status: { code: "new", label: "Новая" },
          source: "cart",
          itemsCount: 2,
          customerComment: null,
          createdAt: "2026-05-07T12:30:00+03:00",
        },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });
    requestsApiMock.getCustomerRequest.mockResolvedValue({
      number: "ЗК26-0001",
      status: { code: "new", label: "Новая" },
      source: "cart",
      customerComment: "Нужна поставка партиями",
      internalComment: "Позвонить перед отгрузкой",
      createdAt: "2026-05-07T12:30:00+03:00",
      customer: {
        name: "Иван Петров",
        email: "ivan@example.com",
        phone: "+79000000000",
      },
      organization: null,
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
    });

    renderPage();

    await user.click(await screen.findByRole("button", { name: "Быстрый просмотр ЗК26-0001" }));

    expect(requestsApiMock.getCustomerRequest).toHaveBeenCalledWith("ЗК26-0001");
    expect(await screen.findByRole("dialog", { name: "Быстрый просмотр ЗК26-0001" })).toBeInTheDocument();
    expect(screen.getByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    expect(screen.getByText("Заявка создана.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Открыть полностью" })).toHaveAttribute(
      "href",
      "/account/requests/%D0%97%D0%9A26-0001",
    );
    expect(screen.queryByText("Позвонить перед отгрузкой")).not.toBeInTheDocument();
  });

  it("redirects unauthorized users to login", async () => {
    authApiMock.getMe.mockRejectedValue(
      new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }),
    );

    renderPage();

    await waitFor(() => expect(routerMock.push).toHaveBeenCalledWith("/auth/login?returnTo=%2Faccount%2Frequests"));
  });
});
