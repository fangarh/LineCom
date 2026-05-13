import { afterEach, describe, expect, it } from "vitest";
import robots from "./robots";

const originalOrigin = process.env.LINECOM_PUBLIC_SITE_ORIGIN;

afterEach(() => {
  if (originalOrigin === undefined) {
    delete process.env.LINECOM_PUBLIC_SITE_ORIGIN;
    return;
  }
  process.env.LINECOM_PUBLIC_SITE_ORIGIN = originalOrigin;
});

describe("robots route", () => {
  it("allows public pages and blocks internal authenticated surfaces", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru";

    expect(robots()).toEqual({
      rules: {
        userAgent: "*",
        allow: "/",
        disallow: ["/admin/", "/account/", "/auth/"],
      },
      sitemap: "https://linecom.example.ru/sitemap.xml",
      host: "https://linecom.example.ru",
    });
  });
});
