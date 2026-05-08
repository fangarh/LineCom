import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { ApiClientError } from "@/lib/api/errors";
import { RequestDetailPageClient } from "./request-detail-page-client";

const routerMock = vi.hoisted(() => ({
  push: vi.fn(),
}));

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
}));

const requestsApiMock = vi.hoisted(() => ({
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
    getCustomerRequest: requestsApiMock.getCustomerRequest,
  };
});

function renderPage(number = "ЗК26-0001") {
  render(
    <AuthProvider>
      <RequestDetailPageClient number={number} />
    </AuthProvider>,
  );
}

describe("RequestDetailPageClient", () => {
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

  it("loads request detail by public number", async () => {
    requestsApiMock.getCustomerRequest.mockResolvedValue({
      number: "ЗК26-0001",
      status: { code: "new", label: "Новая" },
      source: "cart",
      customerComment: null,
      createdAt: "2026-05-07T12:30:00+03:00",
      items: [],
    });

    renderPage();

    expect(await screen.findByRole("heading", { name: "Заявка ЗК26-0001" })).toBeInTheDocument();
    expect(requestsApiMock.getCustomerRequest).toHaveBeenCalledWith("ЗК26-0001");
  });

  it("shows controlled not-found state", async () => {
    requestsApiMock.getCustomerRequest.mockRejectedValue(
      new ApiClientError(404, { code: "request.not_found", message: "Заявка не найдена." }),
    );

    renderPage();

    expect(screen.getByRole("heading", { name: "Карточка заявки" })).toBeInTheDocument();
    expect(screen.getByText("ЗК26-0001")).toBeInTheDocument();
    expect(await screen.findByText("Заявка не найдена.")).toBeInTheDocument();
    expect(routerMock.push).not.toHaveBeenCalled();
  });

  it("redirects unauthorized users to login with return path", async () => {
    authApiMock.getMe.mockRejectedValue(
      new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }),
    );

    renderPage("ЗК26-0001");

    await waitFor(() =>
      expect(routerMock.push).toHaveBeenCalledWith(
        "/auth/login?returnTo=%2Faccount%2Frequests%2F%D0%97%D0%9A26-0001",
      ),
    );
  });
});
