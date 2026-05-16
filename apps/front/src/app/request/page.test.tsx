import { useEffect, type ReactNode } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { Metadata } from "next";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "@/components/auth/auth-provider";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { ApiClientError } from "@/lib/api/errors";
import type { AuthSession } from "@/lib/api/auth";
import type { CustomerRequestDetail } from "@/lib/api/requests";
import type { RequestDraftState } from "@/lib/request-draft/types";
import RequestPage, * as pageModule from "./page";

const routerMock = vi.hoisted(() => ({
  push: vi.fn(),
}));

const requestApiMock = vi.hoisted(() => ({
  createCustomerRequest: vi.fn(),
}));

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => routerMock,
}));

vi.mock("@/lib/api/requests", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/requests")>();
  return {
    ...actual,
    createCustomerRequest: requestApiMock.createCustomerRequest,
  };
});

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();
  return {
    ...actual,
    getMe: authApiMock.getMe,
  };
});

const draft: RequestDraftState = {
  customerComment: "Нужна поставка партиями",
  items: [
    {
      productId: "11111111-1111-1111-1111-111111111111",
      slug: "u-utp-cat-5e",
      productName: "Кабель U/UTP Cat 5e",
      productSku: "LC-UTP5E",
      saleUnit: { code: "coil", label: "бухта" },
      unitQuantity: "305 м",
      quantity: 2,
      customerComment: "Согласовать цвет",
    },
  ],
};

const user = {
  id: "22222222-2222-2222-2222-222222222222",
  name: "Иван Петров",
  email: "client@example.com",
  phone: null,
  role: "customer",
};

const session: AuthSession = {
  user,
  csrfToken: "csrf-1",
};

const createdRequest: CustomerRequestDetail = {
  number: "LC-2026-0001",
  status: { code: "new", label: "Новая" },
  source: "cart",
  customerComment: "Нужна поставка партиями",
  createdAt: "2026-05-07T12:00:00Z",
  items: [],
};

function AuthSeeder({ session, children }: { session?: AuthSession; children: ReactNode }) {
  const auth = useAuth();

  useEffect(() => {
    if (session) {
      auth.setSession(session);
    }
  }, [auth, session]);

  return children;
}

function renderRequestPage(authSession?: AuthSession) {
  localStorage.setItem("linecom.requestDraft.v1", JSON.stringify(draft));

  render(
    <AuthProvider>
      <AuthSeeder session={authSession}>
        <RequestDraftProvider>
          <RequestPage />
        </RequestDraftProvider>
      </AuthSeeder>
    </AuthProvider>,
  );
}

describe("RequestPage", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("redirects anonymous users to login without submitting the draft", async () => {
    const userEventApi = userEvent.setup();
    renderRequestPage();

    await screen.findByText("Кабель U/UTP Cat 5e");
    await userEventApi.click(screen.getByRole("button", { name: "Отправить заявку" }));

    expect(requestApiMock.createCustomerRequest).not.toHaveBeenCalled();
    expect(routerMock.push).toHaveBeenCalledWith("/auth/login?returnTo=%2Frequest");
  });

  it("submits an authenticated draft and redirects to the created request", async () => {
    const userEventApi = userEvent.setup();
    requestApiMock.createCustomerRequest.mockResolvedValue(createdRequest);
    renderRequestPage(session);

    await screen.findByText("Кабель U/UTP Cat 5e");
    await userEventApi.click(screen.getByRole("button", { name: "Отправить заявку" }));

    await waitFor(() =>
      expect(requestApiMock.createCustomerRequest).toHaveBeenCalledWith(
        {
          source: "cart",
          customerComment: "Нужна поставка партиями",
          items: [
            {
              productId: "11111111-1111-1111-1111-111111111111",
              quantity: 2,
              customerComment: "Согласовать цвет",
            },
          ],
        },
        "csrf-1",
      ),
    );
    expect(routerMock.push).toHaveBeenCalledWith("/account/requests/LC-2026-0001");
    expect(screen.getByText("В заявке пока нет товаров")).toBeInTheDocument();
  });

  it("refreshes csrf and retries once on auth.forbidden", async () => {
    const userEventApi = userEvent.setup();
    requestApiMock.createCustomerRequest
      .mockRejectedValueOnce(new ApiClientError(403, { code: "auth.forbidden", message: "Обновите сессию." }))
      .mockResolvedValueOnce(createdRequest);
    authApiMock.getMe.mockResolvedValue({ user, csrfToken: "csrf-2" });
    renderRequestPage(session);

    await screen.findByText("Кабель U/UTP Cat 5e");
    await userEventApi.click(screen.getByRole("button", { name: "Отправить заявку" }));

    await waitFor(() => expect(requestApiMock.createCustomerRequest).toHaveBeenCalledTimes(2));
    expect(authApiMock.getMe).toHaveBeenCalledTimes(1);
    expect(requestApiMock.createCustomerRequest).toHaveBeenNthCalledWith(2, expect.any(Object), "csrf-2");
    expect(routerMock.push).toHaveBeenCalledWith("/account/requests/LC-2026-0001");
  });

  it("keeps unavailable products in the draft and shows a controlled backend message", async () => {
    const userEventApi = userEvent.setup();
    requestApiMock.createCustomerRequest.mockRejectedValue(
      new ApiClientError(409, { code: "request.product_not_available", message: "Товар временно недоступен." }),
    );
    renderRequestPage(session);

    await screen.findByText("Кабель U/UTP Cat 5e");
    await userEventApi.click(screen.getByRole("button", { name: "Отправить заявку" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Товар временно недоступен.");
    expect(screen.getByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    expect(routerMock.push).not.toHaveBeenCalled();
  });
});

describe("request route metadata", () => {
  it("marks the request draft page as noindex instead of inheriting the homepage canonical", () => {
    const metadata = (pageModule as { metadata?: Metadata }).metadata;

    expect(metadata).toMatchObject({
      alternates: {
        canonical: "/request",
      },
      robots: {
        index: false,
        follow: false,
      },
    });
  });
});
