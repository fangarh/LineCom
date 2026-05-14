import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiClientError } from "./errors";
import { apiForm, apiJson } from "./http";

function jsonResponse(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function textResponse(body: string, status = 500) {
  return new Response(body, {
    status,
    headers: { "Content-Type": "text/html" },
  });
}

function emptyResponse(status = 500) {
  return new Response(null, { status });
}

function malformedJsonResponse(status = 500) {
  return new Response("{ nope", {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

async function expectInvalidResponse(promise: Promise<unknown>, status = 500) {
  await expect(promise).rejects.toMatchObject({
    status,
    code: "transport.invalid_response",
    message: "Не удалось обработать ответ сервера. Попробуйте позже.",
  });
}

describe("api http helpers", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("apiJson throws a controlled invalid response error for non-json error bodies", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(textResponse("<html>bad gateway</html>")));

    await expectInvalidResponse(apiJson("/api/catalog"));
  });

  it("apiJson throws a controlled invalid response error for empty non-204 bodies", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(emptyResponse()));

    await expectInvalidResponse(apiJson("/api/catalog"));
  });

  it("apiJson throws a controlled invalid response error for malformed json bodies", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(malformedJsonResponse()));

    await expectInvalidResponse(apiJson("/api/catalog"));
  });

  it("apiJson preserves valid backend API error codes and messages", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(jsonResponse({ code: "auth.unauthorized", message: "Требуется вход." }, 401)),
    );

    await expect(apiJson("/api/auth/me")).rejects.toMatchObject({
      status: 401,
      code: "auth.unauthorized",
      message: "Требуется вход.",
    });
  });

  it("apiJson preserves existing fallback for valid json error bodies with unknown shape", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse({ error: "unexpected" }, 500)));

    await expect(apiJson("/api/catalog")).rejects.toMatchObject({
      status: 500,
      code: "internal_error",
      message: "Внутренняя ошибка сервера.",
    });
  });

  it("apiJson returns undefined for no-content responses", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(emptyResponse(204)));

    await expect(apiJson("/api/catalog")).resolves.toBeUndefined();
  });

  it("apiJson throws a controlled invalid response error for malformed successful bodies", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(malformedJsonResponse(200)));

    await expectInvalidResponse(apiJson("/api/catalog"), 200);
  });

  it("apiForm uses the same invalid response parsing path for multipart requests", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(textResponse("proxy failure")));

    const formData = new FormData();
    formData.set("file", new File(["logo"], "logo.png", { type: "image/png" }));

    await expectInvalidResponse(apiForm("/api/admin/catalog/brands/brand-id/logo", { method: "PUT", body: formData }));
  });

  it("invalid response errors carry diagnostics without exposing them as the message", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(textResponse("<html>upstream</html>")));

    await apiJson("/api/catalog").catch((error: unknown) => {
      expect(error).toBeInstanceOf(ApiClientError);
      const apiError = error as ApiClientError;
      expect(apiError.message).toBe("Не удалось обработать ответ сервера. Попробуйте позже.");
      expect(apiError.diagnostics?.body).toContain("upstream");
    });
  });
});
