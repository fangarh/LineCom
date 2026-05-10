import { afterEach, describe, expect, it, vi } from "vitest";
import { routes } from "../routes";
import {
  getAdminRequest,
  getAdminRequests,
  updateAdminRequestInternalComment,
  updateAdminRequestStatus,
} from "./admin-requests";

function jsonResponse(payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

function expectJsonHeaders(headers: Headers, csrfToken?: string) {
  expect(headers.get("Accept")).toBe("application/json");
  expect(headers.get("Content-Type")).toBe("application/json");
  if (csrfToken) {
    expect(headers.get("X-CSRF-Token")).toBe(csrfToken);
  }
}

describe("admin request API client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("builds admin request routes with encoded numbers", () => {
    expect(routes.adminRequests()).toBe("/admin/requests");
    expect(routes.adminRequest("REQ/42")).toBe("/admin/requests/REQ%2F42");
  });

  it("passes filtered list params and disables cache", async () => {
    const payload = { items: [], page: 2, pageSize: 25, totalItems: 0, totalPages: 0 };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      getAdminRequests({
        page: 2,
        pageSize: 25,
        status: "new",
        number: "REQ 1/2",
        contact: "client@example.com",
        organization: "Acme & Co",
      }),
    ).resolves.toEqual(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/requests?page=2&pageSize=25&status=new&number=REQ+1%2F2&contact=client%40example.com&organization=Acme+%26+Co",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
  });

  it("gets one request by encoded number and disables cache", async () => {
    const payload = { number: "REQ/42" };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    await expect(getAdminRequest("REQ/42")).resolves.toEqual(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/requests/REQ%2F42",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
  });

  it("patches status with csrf token and json body", async () => {
    const payload = { number: "REQ/42", status: { code: "in_progress", label: "In progress" } };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    await expect(updateAdminRequestStatus("REQ/42", "in_progress", "csrf-token")).resolves.toEqual(payload);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/requests/REQ%2F42/status");
    expect(init.method).toBe("PATCH");
    expect(init.credentials).toBe("include");
    expect(init.body).toBe(JSON.stringify({ status: "in_progress" }));
    expectJsonHeaders(init.headers as Headers, "csrf-token");
  });

  it("puts internal comment with csrf token and json body", async () => {
    const payload = { number: "REQ-42", internalComment: "Call before noon" };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    await expect(updateAdminRequestInternalComment("REQ-42", "Call before noon", "csrf-token")).resolves.toEqual(payload);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/requests/REQ-42/internal-comment");
    expect(init.method).toBe("PUT");
    expect(init.credentials).toBe("include");
    expect(init.body).toBe(JSON.stringify({ internalComment: "Call before noon" }));
    expectJsonHeaders(init.headers as Headers, "csrf-token");
  });
});
