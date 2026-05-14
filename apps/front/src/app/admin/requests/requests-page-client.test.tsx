import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import type { AdminRequestDetail as AdminRequestDetailModel } from "@/lib/api/admin-requests";
import { ApiClientError } from "@/lib/api/errors";
import { RequestsPageClient } from "./requests-page-client";

const routerMock = vi.hoisted(() => ({
  push: vi.fn(),
}));

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
}));

const adminRequestsApiMock = vi.hoisted(() => ({
  getAdminRequests: vi.fn(),
  getAdminRequest: vi.fn(),
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

vi.mock("@/lib/api/admin-requests", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-requests")>();
  return {
    ...actual,
    getAdminRequests: adminRequestsApiMock.getAdminRequests,
    getAdminRequest: adminRequestsApiMock.getAdminRequest,
  };
});

const requestDetail: AdminRequestDetailModel = {
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
  ],
};

function renderPage() {
  return render(
    <AuthProvider>
      <RequestsPageClient />
    </AuthProvider>,
  );
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (error: unknown) => void;
  const promise = new Promise<T>((promiseResolve, promiseReject) => {
    resolve = promiseResolve;
    reject = promiseReject;
  });

  return { promise, resolve, reject };
}

describe("Admin RequestsPageClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "11111111-1111-1111-1111-111111111111",
        name: "Анна",
        email: "anna@example.com",
        phone: null,
        role: "seller",
      },
      csrfToken: "csrf",
    });
    adminRequestsApiMock.getAdminRequests.mockResolvedValue({
      items: [
        {
          number: "ЗК26-0001",
          status: { code: "new", label: "Новая" },
          source: "cart",
          itemsCount: 2,
          customer: { name: "Иван Петров", email: "ivan@example.com", phone: null },
          organization: { name: "ООО Вектор", inn: null, contactPerson: null },
          customerComment: null,
          internalComment: null,
          createdAt: "2026-05-07T12:30:00+03:00",
          updatedAt: "2026-05-07T12:30:00+03:00",
        },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });
    adminRequestsApiMock.getAdminRequest.mockResolvedValue(requestDetail);
  });

  it("loads admin requests for seller", async () => {
    renderPage();

    expect(await screen.findByRole("heading", { name: "ЗК26-0001" })).toBeInTheDocument();
    expect(authApiMock.getMe).toHaveBeenCalledTimes(1);
    expect(adminRequestsApiMock.getAdminRequests).toHaveBeenCalledWith({
      status: undefined,
      number: undefined,
      contact: undefined,
      organization: undefined,
    });
  });

  it("shows forbidden message for customer", async () => {
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "22222222-2222-2222-2222-222222222222",
        name: "Иван",
        email: "ivan@example.com",
        phone: null,
        role: "customer",
      },
      csrfToken: "csrf",
    });

    renderPage();

    expect(await screen.findByText("У вас нет доступа к очереди заявок.")).toBeInTheDocument();
    expect(adminRequestsApiMock.getAdminRequests).not.toHaveBeenCalled();
  });

  it("redirects unauthorized users to login with returnTo", async () => {
    authApiMock.getMe.mockRejectedValue(
      new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }),
    );

    renderPage();

    await waitFor(() => expect(routerMock.push).toHaveBeenCalledWith("/auth/login?returnTo=%2Fadmin%2Frequests"));
  });

  it("does not redirect from a stale unauthorized response after unmount", async () => {
    const sessionRequest = deferred<never>();
    authApiMock.getMe.mockReturnValue(sessionRequest.promise);

    const { unmount } = renderPage();
    unmount();
    sessionRequest.reject(new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }));
    await sessionRequest.promise.catch(() => undefined);
    await Promise.resolve();

    expect(routerMock.push).not.toHaveBeenCalled();
  });

  it("keeps filters mounted while refreshing the list", async () => {
    const refreshRequest = deferred<Awaited<ReturnType<typeof adminRequestsApiMock.getAdminRequests>>>();
    adminRequestsApiMock.getAdminRequests
      .mockResolvedValueOnce({
        items: [
          {
            number: "ЗК26-0001",
            status: { code: "new", label: "Новая" },
            source: "cart",
            itemsCount: 2,
            customer: { name: "Иван Петров", email: "ivan@example.com", phone: null },
            organization: { name: "ООО Вектор", inn: null, contactPerson: null },
            customerComment: null,
            internalComment: null,
            createdAt: "2026-05-07T12:30:00+03:00",
            updatedAt: "2026-05-07T12:30:00+03:00",
          },
        ],
        page: 1,
        pageSize: 20,
        totalItems: 1,
        totalPages: 1,
      })
      .mockReturnValueOnce(refreshRequest.promise);

    renderPage();

    const numberFilter = await screen.findByLabelText("Номер");
    await userEvent.click(numberFilter);
    fireEvent.change(numberFilter, { target: { value: "0001" } });

    await waitFor(() => expect(screen.getByText("Обновляем список заявок...")).toBeInTheDocument());
    expect(screen.getByLabelText("Номер")).toHaveValue("0001");
    expect(screen.getByLabelText("Номер")).toHaveFocus();
  });

  it("loads admin request detail into a quick preview drawer without mutation controls", async () => {
    const user = userEvent.setup();

    renderPage();

    await user.click(await screen.findByRole("button", { name: "Быстрый просмотр ЗК26-0001" }));

    expect(adminRequestsApiMock.getAdminRequest).toHaveBeenCalledWith("ЗК26-0001");
    const preview = await screen.findByRole("dialog", { name: "Быстрый просмотр ЗК26-0001" });
    const previewScope = within(preview);
    expect(previewScope.getByRole("heading", { name: "Контактный снимок" })).toBeInTheDocument();
    expect(previewScope.getByText("Иван Петров")).toBeInTheDocument();
    expect(previewScope.getByText("ivan@example.com")).toBeInTheDocument();
    expect(previewScope.getByRole("heading", { name: "Организация" })).toBeInTheDocument();
    expect(previewScope.getByText("ООО Кабельные системы")).toBeInTheDocument();
    expect(previewScope.getByText("7700000000")).toBeInTheDocument();
    expect(previewScope.getByText("Уточнить наличие на складе")).toBeInTheDocument();
    expect(previewScope.getByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    expect(previewScope.getByText("LC-UTP5E")).toBeInTheDocument();
    expect(previewScope.getByText("2 бухта")).toBeInTheDocument();
    expect(previewScope.getByText("Заявка создана.")).toBeInTheDocument();
    expect(previewScope.getByRole("link", { name: "Открыть обработку" })).toHaveAttribute(
      "href",
      "/admin/requests/%D0%97%D0%9A26-0001",
    );
    expect(previewScope.queryByRole("button", { name: "Сохранить статус" })).not.toBeInTheDocument();
    expect(previewScope.queryByRole("button", { name: "Сохранить комментарий" })).not.toBeInTheDocument();
  });
});
