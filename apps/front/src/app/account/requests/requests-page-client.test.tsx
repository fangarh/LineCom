import { render, screen, waitFor } from "@testing-library/react";
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

  it("redirects unauthorized users to login", async () => {
    authApiMock.getMe.mockRejectedValue(
      new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }),
    );

    renderPage();

    await waitFor(() => expect(routerMock.push).toHaveBeenCalledWith("/auth/login?returnTo=%2Faccount%2Frequests"));
  });
});
