import { describe, expect, it } from "vitest";
import { isApiErrorResponse, normalizeApiError } from "./errors";

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
});
