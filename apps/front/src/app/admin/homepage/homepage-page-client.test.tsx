import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { ApiClientError } from "@/lib/api/errors";
import { HomepagePageClient } from "./homepage-page-client";

const routerPushMock = vi.hoisted(() => vi.fn());

const getMeMock = vi.hoisted(() => vi.fn());

const adminHomepageApiMock = vi.hoisted(() => ({
  getAdminHomepageSections: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: routerPushMock,
  }),
}));

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();
  return {
    ...actual,
    getMe: getMeMock,
  };
});

vi.mock("@/lib/api/admin-homepage", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-homepage")>();
  return {
    ...actual,
    getAdminHomepageSections: adminHomepageApiMock.getAdminHomepageSections,
  };
});

function renderPage() {
  return render(
    <AuthProvider>
      <HomepagePageClient />
    </AuthProvider>,
  );
}

describe("HomepagePageClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getMeMock.mockResolvedValue({
      user: { id: "u1", name: "Seller", email: null, phone: null, role: "seller" },
      csrfToken: "csrf",
    });
    adminHomepageApiMock.getAdminHomepageSections.mockResolvedValue({ sections: [] });
  });

  it("redirects unauthorized users to login with returnTo", async () => {
    getMeMock.mockRejectedValue(new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }));

    renderPage();

    await waitFor(() => expect(routerPushMock).toHaveBeenCalledWith("/auth/login?returnTo=%2Fadmin%2Fhomepage"));
    expect(adminHomepageApiMock.getAdminHomepageSections).not.toHaveBeenCalled();
  });

  it("shows forbidden state for customer role", async () => {
    getMeMock.mockResolvedValue({
      user: { id: "u1", name: "Customer", email: null, phone: null, role: "customer" },
      csrfToken: "csrf",
    });

    renderPage();

    expect(await screen.findByText("У вас нет доступа к управлению главной страницей.")).toBeInTheDocument();
    expect(adminHomepageApiMock.getAdminHomepageSections).not.toHaveBeenCalled();
  });
});
