import { act, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AuthProvider, useAuth } from "./auth-provider";
import { getMe } from "@/lib/api/auth";
import type { AuthSession } from "@/lib/api/auth";

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();
  return {
    ...actual,
    getMe: vi.fn(),
  };
});

const getMeMock = vi.mocked(getMe);

const session: AuthSession = {
  user: {
    id: "user-1",
    name: "Покупатель",
    email: "customer@linecom.test",
    phone: null,
    role: "customer",
  },
  csrfToken: "csrf-1",
};

function AuthProbe() {
  const auth = useAuth();

  return (
    <section>
      <div data-testid="status">{auth.status}</div>
      <div data-testid="user">{auth.user?.name ?? "anonymous"}</div>
      <div data-testid="csrf">{auth.csrfToken ?? "none"}</div>
      <button type="button" onClick={() => void auth.restoreSession()}>
        Restore
      </button>
      <button type="button" onClick={() => auth.clearSession()}>
        Clear
      </button>
    </section>
  );
}

describe("AuthProvider", () => {
  it("restores the current session through getMe on mount", async () => {
    getMeMock.mockResolvedValue(session);

    render(
      <AuthProvider>
        <AuthProbe />
      </AuthProvider>,
    );

    expect(screen.getByTestId("status")).toHaveTextContent("restoring");

    await waitFor(() => expect(screen.getByTestId("status")).toHaveTextContent("authenticated"));
    expect(screen.getByTestId("user")).toHaveTextContent("Покупатель");
    expect(screen.getByTestId("csrf")).toHaveTextContent("csrf-1");
    expect(getMeMock).toHaveBeenCalledTimes(1);
  });

  it("treats auth.unauthorized as anonymous without throwing a page-level error", async () => {
    getMeMock.mockRejectedValue(
      new ApiClientError(401, {
        code: "auth.unauthorized",
        message: "Требуется вход.",
      }),
    );

    render(
      <AuthProvider>
        <AuthProbe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId("status")).toHaveTextContent("anonymous"));
    expect(screen.getByTestId("user")).toHaveTextContent("anonymous");
    expect(screen.getByTestId("csrf")).toHaveTextContent("none");
  });

  it("keeps setSession and clearSession behavior for explicit auth flows", async () => {
    getMeMock.mockResolvedValue(session);

    render(
      <AuthProvider initialSession={session}>
        <AuthProbe />
      </AuthProvider>,
    );

    expect(screen.getByTestId("status")).toHaveTextContent("authenticated");
    expect(screen.getByTestId("user")).toHaveTextContent("Покупатель");
    expect(getMeMock).not.toHaveBeenCalled();

    await act(async () => {
      screen.getByRole("button", { name: "Clear" }).click();
    });

    expect(screen.getByTestId("status")).toHaveTextContent("anonymous");
    expect(screen.getByTestId("user")).toHaveTextContent("anonymous");
    expect(screen.getByTestId("csrf")).toHaveTextContent("none");
  });
});
