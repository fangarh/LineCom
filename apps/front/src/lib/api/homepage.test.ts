import { afterEach, describe, expect, it, vi } from "vitest";
import { getHomepageSections } from "./homepage";

function jsonResponse(payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

describe("public homepage API client", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("gets public homepage sections with ISR revalidation", async () => {
    const payload = { sections: [] };
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));

    await expect(getHomepageSections()).resolves.toEqual(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/public/homepage/sections",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        next: { revalidate: 60 },
      }),
    );
  });
});
