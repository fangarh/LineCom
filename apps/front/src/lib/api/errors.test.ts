import { describe, expect, it } from "vitest";
import { ApiClientError, invalidApiResponseError, isApiErrorResponse, normalizeApiError } from "./errors";

describe("api errors", () => {
  it("accepts backend ApiErrorResponse shape", () => {
    expect(isApiErrorResponse({ code: "auth.unauthorized", message: "Требуется вход." })).toBe(true);
  });

  it("rejects unknown payloads", () => {
    expect(isApiErrorResponse({ error: "nope" })).toBe(false);
    expect(isApiErrorResponse(null)).toBe(false);
  });

  it("normalizes non-api failures to internal_error", () => {
    expect(normalizeApiError(new Error("network")).code).toBe("internal_error");
  });

  it("normalizes invalid response errors without exposing diagnostics", () => {
    const error = new ApiClientError(502, invalidApiResponseError, {
      reason: "malformed_json",
      status: 502,
      body: "<html>bad gateway</html>",
      parseError: "Unexpected token '<'",
    });

    expect(error.diagnostics?.body).toContain("bad gateway");
    expect(normalizeApiError(error)).toEqual({
      code: "transport.invalid_response",
      message: "Не удалось обработать ответ сервера. Попробуйте позже.",
    });
    expect(normalizeApiError(error).message).not.toContain("bad gateway");
    expect(normalizeApiError(error).message).not.toContain("Unexpected token");
  });
});
