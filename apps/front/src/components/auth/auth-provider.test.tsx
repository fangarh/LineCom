import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, afterEach } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AuthProvider, useAuth } from "./auth-provider";
import type { AuthSession } from "@/lib/api/auth";

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
  logout: vi.fn(),
}));

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();

  return {
    ...actual,
    getMe: authApiMock.getMe,
    logout: authApiMock.logout,
  };
});

const customerSession: AuthSession = {
  csrfToken: "customer-csrf-token",
  user: {
    id: "customer-id",
    name: "Ivan Petrov",
    email: "ivan@example.com",
    phone: "+79000000000",
    role: "customer",
  },
};

function createDeferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((promiseResolve, promiseReject) => {
    resolve = promiseResolve;
    reject = promiseReject;
  });

  return { promise, resolve, reject };
}

function AuthProbe() {
  const { user, setSession, restoreSession, logoutSession } = useAuth();

  return (
    <>
      <p data-testid="auth-user">{user?.email ?? "anonymous"}</p>
      <button type="button" onClick={() => void restoreSession()}>
        Restore
      </button>
      <button type="button" onClick={() => setSession(customerSession)}>
        Login
      </button>
      <button type="button" onClick={() => void logoutSession()}>
        Logout
      </button>
    </>
  );
}

function renderProbe() {
  render(
    <AuthProvider>
      <AuthProbe />
    </AuthProvider>,
  );
}

describe("AuthProvider", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it("does not clear a newer login when an older restore rejects as unauthorized", async () => {
    const deferredRestore = createDeferred<AuthSession>();
    authApiMock.getMe.mockReturnValue(deferredRestore.promise);
    const user = userEvent.setup();

    renderProbe();

    await user.click(screen.getByRole("button", { name: "Restore" }));
    await waitFor(() => expect(authApiMock.getMe).toHaveBeenCalledTimes(1));
    await user.click(screen.getByRole("button", { name: "Login" }));
    expect(screen.getByTestId("auth-user")).toHaveTextContent("ivan@example.com");

    await act(async () => {
      deferredRestore.reject(new ApiClientError(401, { code: "auth.unauthorized", message: "Unauthorized" }));
    });

    expect(screen.getByTestId("auth-user")).toHaveTextContent("ivan@example.com");
  });

  it("does not restore an older session after logout completes", async () => {
    const deferredRestore = createDeferred<AuthSession>();
    authApiMock.getMe.mockReturnValue(deferredRestore.promise);
    authApiMock.logout.mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderProbe();

    await user.click(screen.getByRole("button", { name: "Login" }));
    await user.click(screen.getByRole("button", { name: "Restore" }));
    await waitFor(() => expect(authApiMock.getMe).toHaveBeenCalledTimes(1));
    await user.click(screen.getByRole("button", { name: "Logout" }));
    await waitFor(() => expect(screen.getByTestId("auth-user")).toHaveTextContent("anonymous"));

    await act(async () => {
      deferredRestore.resolve(customerSession);
    });

    expect(screen.getByTestId("auth-user")).toHaveTextContent("anonymous");
  });
});
