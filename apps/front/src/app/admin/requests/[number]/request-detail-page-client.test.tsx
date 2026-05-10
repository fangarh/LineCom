import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { ApiClientError } from "@/lib/api/errors";
import { AdminRequestDetailPageClient } from "./request-detail-page-client";

const routerMock = vi.hoisted(() => ({
  push: vi.fn(),
}));

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
}));

const adminRequestsApiMock = vi.hoisted(() => ({
  getAdminRequest: vi.fn(),
  updateAdminRequestStatus: vi.fn(),
  updateAdminRequestInternalComment: vi.fn(),
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
    getAdminRequest: adminRequestsApiMock.getAdminRequest,
    updateAdminRequestStatus: adminRequestsApiMock.updateAdminRequestStatus,
    updateAdminRequestInternalComment: adminRequestsApiMock.updateAdminRequestInternalComment,
  };
});

const request = {
  number: "ЗК26-0001",
  status: { code: "new", label: "Новая" },
  source: "cart",
  itemsCount: 1,
  customer: { name: "Иван Петров", email: "ivan@example.com", phone: null },
  organization: { name: "ООО Кабельные системы", inn: "7700000000", contactPerson: null },
  customerComment: null,
  internalComment: "Первичная проверка",
  createdAt: "2026-05-07T12:30:00+03:00",
  updatedAt: "2026-05-07T12:30:00+03:00",
  items: [
    {
      productId: "11111111-1111-1111-1111-111111111111",
      productName: "Кабель U/UTP Cat 5e",
      productSku: "LC-UTP5E",
      saleUnit: { code: "coil", label: "бухта" },
      unitQuantity: "305 м",
      quantity: 2,
      customerComment: null,
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

const nextRequest = {
  ...request,
  number: "ЗК26-0002",
  status: { code: "new", label: "Новая" },
  customer: { name: "Мария Орлова", email: "maria@example.com", phone: null },
  organization: { name: "ООО Линия связи", inn: "7800000000", contactPerson: null },
  internalComment: "Новая карточка",
  createdAt: "2026-05-09T10:15:00+03:00",
  updatedAt: "2026-05-09T10:15:00+03:00",
};

function renderPage(number = "ЗК26-0001") {
  return render(
    <AuthProvider>
      <AdminRequestDetailPageClient number={number} />
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

describe("AdminRequestDetailPageClient", () => {
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
      csrfToken: "csrf-token",
    });
    adminRequestsApiMock.getAdminRequest.mockResolvedValue(request);
    adminRequestsApiMock.updateAdminRequestStatus.mockResolvedValue({
      ...request,
      status: { code: "in_progress", label: "В работе" },
      updatedAt: "2026-05-08T10:00:00+03:00",
    });
    adminRequestsApiMock.updateAdminRequestInternalComment.mockResolvedValue({
      ...request,
      internalComment: "Связаться с клиентом",
      updatedAt: "2026-05-08T10:30:00+03:00",
    });
  });

  it("loads detail for seller", async () => {
    renderPage();

    expect(await screen.findByRole("heading", { name: "Заявка ЗК26-0001" })).toBeInTheDocument();
    expect(authApiMock.getMe).toHaveBeenCalledTimes(1);
    expect(adminRequestsApiMock.getAdminRequest).toHaveBeenCalledWith("ЗК26-0001");
  });

  it("shows forbidden state for customer", async () => {
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "22222222-2222-2222-2222-222222222222",
        name: "Иван",
        email: "ivan@example.com",
        phone: null,
        role: "customer",
      },
      csrfToken: "csrf-token",
    });

    renderPage();

    expect(await screen.findByText("У вас нет доступа к карточке заявки.")).toBeInTheDocument();
    expect(adminRequestsApiMock.getAdminRequest).not.toHaveBeenCalled();
  });

  it("redirects unauthorized users to login", async () => {
    authApiMock.getMe.mockRejectedValue(
      new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }),
    );

    renderPage("ЗК26-0001");

    await waitFor(() =>
      expect(routerMock.push).toHaveBeenCalledWith(
        "/auth/login?returnTo=%2Fadmin%2Frequests%2F%25D0%2597%25D0%259A26-0001",
      ),
    );
  });

  it("does not redirect from stale unauthorized response after unmount", async () => {
    const sessionRequest = deferred<never>();
    authApiMock.getMe.mockReturnValue(sessionRequest.promise);

    const { unmount } = renderPage();
    unmount();
    sessionRequest.reject(new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }));
    await sessionRequest.promise.catch(() => undefined);
    await Promise.resolve();

    expect(routerMock.push).not.toHaveBeenCalled();
  });

  it("saves status with csrf token", async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByRole("heading", { name: "Заявка ЗК26-0001" });
    await user.selectOptions(screen.getByLabelText("Статус"), "in_progress");
    await user.click(screen.getByRole("button", { name: "Сохранить статус" }));

    await waitFor(() =>
      expect(adminRequestsApiMock.updateAdminRequestStatus).toHaveBeenCalledWith(
        "ЗК26-0001",
        "in_progress",
        "csrf-token",
      ),
    );
    expect(await screen.findByText("Статус сохранен.")).toBeInTheDocument();
    expect(screen.getAllByText("В работе").length).toBeGreaterThan(0);
  });

  it("does not start a second status mutation while the first one is pending", async () => {
    const statusRequest = deferred<typeof request>();
    adminRequestsApiMock.updateAdminRequestStatus.mockReturnValue(statusRequest.promise);
    renderPage();

    await screen.findByRole("heading", { name: "Заявка ЗК26-0001" });
    await userEvent.selectOptions(screen.getByLabelText("Статус"), "in_progress");
    const saveButton = screen.getByRole("button", { name: "Сохранить статус" });

    fireEvent.click(saveButton);
    fireEvent.click(saveButton);

    expect(adminRequestsApiMock.updateAdminRequestStatus).toHaveBeenCalledTimes(1);
    statusRequest.resolve({
      ...request,
      status: { code: "in_progress", label: "В работе" },
      updatedAt: "2026-05-08T10:00:00+03:00",
    });
    expect(await screen.findByText("Статус сохранен.")).toBeInTheDocument();
  });

  it("does not call mutation APIs when csrf token is missing", async () => {
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "11111111-1111-1111-1111-111111111111",
        name: "Анна",
        email: "anna@example.com",
        phone: null,
        role: "seller",
      },
      csrfToken: "",
    });

    renderPage();

    await screen.findByRole("heading", { name: "Заявка ЗК26-0001" });
    expect(screen.getByText("Сессия не подтверждена. Обновите страницу и войдите снова.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Сохранить статус" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Сохранить комментарий" })).toBeDisabled();
    expect(adminRequestsApiMock.updateAdminRequestStatus).not.toHaveBeenCalled();
    expect(adminRequestsApiMock.updateAdminRequestInternalComment).not.toHaveBeenCalled();
  });

  it("ignores stale status mutation results after unmount", async () => {
    const statusRequest = deferred<typeof request>();
    adminRequestsApiMock.updateAdminRequestStatus.mockReturnValue(statusRequest.promise);
    const { unmount } = renderPage();

    await screen.findByRole("heading", { name: "Заявка ЗК26-0001" });
    await userEvent.selectOptions(screen.getByLabelText("Статус"), "in_progress");
    await userEvent.click(screen.getByRole("button", { name: "Сохранить статус" }));

    unmount();
    statusRequest.resolve({
      ...request,
      status: { code: "in_progress", label: "В работе" },
      updatedAt: "2026-05-08T10:00:00+03:00",
    });
    await statusRequest.promise;
    await Promise.resolve();

    expect(adminRequestsApiMock.updateAdminRequestStatus).toHaveBeenCalledTimes(1);
  });

  it("does not keep the next request disabled when number changes during pending status mutation", async () => {
    const statusRequest = deferred<typeof request>();
    adminRequestsApiMock.getAdminRequest.mockImplementation((number: string) => {
      if (number === "ЗК26-0002") {
        return Promise.resolve(nextRequest);
      }

      return Promise.resolve(request);
    });
    adminRequestsApiMock.updateAdminRequestStatus.mockReturnValue(statusRequest.promise);

    const view = renderPage();

    await screen.findByRole("heading", { name: "Заявка ЗК26-0001" });
    await userEvent.selectOptions(screen.getByLabelText("Статус"), "in_progress");
    await userEvent.click(screen.getByRole("button", { name: "Сохранить статус" }));

    expect(screen.getByRole("button", { name: "Сохранить статус" })).toBeDisabled();

    view.rerender(
      <AuthProvider>
        <AdminRequestDetailPageClient number="ЗК26-0002" />
      </AuthProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Заявка ЗК26-0002" })).toBeInTheDocument();
    expect(screen.getByText("Мария Орлова")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Сохранить статус" })).not.toBeDisabled();

    statusRequest.resolve({
      ...request,
      status: { code: "in_progress", label: "В работе" },
      updatedAt: "2026-05-08T10:00:00+03:00",
    });
    await statusRequest.promise;
    await Promise.resolve();

    expect(screen.getByRole("heading", { name: "Заявка ЗК26-0002" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Заявка ЗК26-0001" })).not.toBeInTheDocument();
    expect(screen.queryByText("Статус сохранен.")).not.toBeInTheDocument();
  });

  it("saves internal comment with csrf token", async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByRole("heading", { name: "Заявка ЗК26-0001" });
    await user.clear(screen.getByLabelText("Внутренний комментарий"));
    await user.type(screen.getByLabelText("Внутренний комментарий"), "Связаться с клиентом");
    await user.click(screen.getByRole("button", { name: "Сохранить комментарий" }));

    await waitFor(() =>
      expect(adminRequestsApiMock.updateAdminRequestInternalComment).toHaveBeenCalledWith(
        "ЗК26-0001",
        "Связаться с клиентом",
        "csrf-token",
      ),
    );
    expect(await screen.findByText("Комментарий сохранен.")).toBeInTheDocument();
    expect(screen.getByLabelText("Внутренний комментарий")).toHaveValue("Связаться с клиентом");
  });
});
