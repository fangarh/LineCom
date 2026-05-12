import { afterEach, describe, expect, it } from "vitest";
import { absoluteSiteUrl, getPublicSiteOrigin, normalizeSiteOrigin } from "./site";

const originalOrigin = process.env.LINECOM_PUBLIC_SITE_ORIGIN;

afterEach(() => {
  if (originalOrigin === undefined) {
    delete process.env.LINECOM_PUBLIC_SITE_ORIGIN;
  } else {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = originalOrigin;
  }
});

describe("site SEO URL helpers", () => {
  it("uses localhost fallback when public origin is not configured", () => {
    delete process.env.LINECOM_PUBLIC_SITE_ORIGIN;

    expect(getPublicSiteOrigin()).toBe("http://127.0.0.1:3000");
  });

  it("normalizes configured public origin by trimming trailing slashes", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru///";

    expect(getPublicSiteOrigin()).toBe("https://linecom.example.ru");
  });

  it("falls back when configured public origin is not an absolute http URL", () => {
    expect(normalizeSiteOrigin("linecom.example.ru")).toBe("http://127.0.0.1:3000");
    expect(normalizeSiteOrigin("ftp://linecom.example.ru")).toBe("http://127.0.0.1:3000");
  });

  it("builds absolute URLs from relative public paths", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/";

    expect(absoluteSiteUrl("/catalog/vitaya-para")).toBe("https://linecom.example.ru/catalog/vitaya-para");
    expect(absoluteSiteUrl("products/u-utp")).toBe("https://linecom.example.ru/products/u-utp");
  });
});
