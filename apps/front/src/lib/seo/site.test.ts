import { afterEach, describe, expect, it, vi } from "vitest";
import { absoluteSiteUrl, getPublicSiteOrigin, normalizeSiteOrigin, siteMetadataBase } from "./site";

const originalOrigin = process.env.LINECOM_PUBLIC_SITE_ORIGIN;
const productionError =
  "LINECOM_PUBLIC_SITE_ORIGIN must be an absolute non-localhost URL in production, e.g. https://line-com.ru";

afterEach(() => {
  vi.unstubAllEnvs();

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

  it("normalizes configured public origin by omitting path query and hash", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/catalog?x=1#top";

    expect(getPublicSiteOrigin()).toBe("https://linecom.example.ru");
  });

  it("falls back when configured public origin is not an absolute http URL", () => {
    expect(normalizeSiteOrigin("linecom.example.ru")).toBe("http://127.0.0.1:3000");
    expect(normalizeSiteOrigin("ftp://linecom.example.ru")).toBe("http://127.0.0.1:3000");
  });

  it("rejects missing or invalid public origins in production", () => {
    expect(() => normalizeSiteOrigin(undefined, "production")).toThrow(productionError);
    expect(() => normalizeSiteOrigin("linecom.example.ru", "production")).toThrow(productionError);
    expect(() => normalizeSiteOrigin("ftp://linecom.example.ru", "production")).toThrow(productionError);
  });

  it("rejects localhost public origins in production", () => {
    expect(() => normalizeSiteOrigin("http://localhost:3000", "production")).toThrow(productionError);
    expect(() => normalizeSiteOrigin("http://127.0.0.1:3000", "production")).toThrow(productionError);
    expect(() => normalizeSiteOrigin("http://[::1]:3000", "production")).toThrow(productionError);
  });

  it("accepts absolute non-localhost public origins in production", () => {
    expect(normalizeSiteOrigin("https://line-com.ru/catalog?x=1#top", "production")).toBe("https://line-com.ru");
    expect(normalizeSiteOrigin("https://preview.example.ru/", "production")).toBe("https://preview.example.ru");
  });

  it("getPublicSiteOrigin fails fast in production when env is missing", () => {
    vi.stubEnv("NODE_ENV", "production");
    vi.stubEnv("LINECOM_PUBLIC_SITE_ORIGIN", "");

    expect(() => getPublicSiteOrigin()).toThrow(productionError);
  });

  it("builds metadata base from the normalized public origin", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/catalog?x=1#top";

    expect(siteMetadataBase().origin).toBe("https://linecom.example.ru");
  });

  it("builds absolute URLs from relative public paths", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/";

    expect(absoluteSiteUrl("/catalog/vitaya-para")).toBe("https://linecom.example.ru/catalog/vitaya-para");
    expect(absoluteSiteUrl("products/u-utp")).toBe("https://linecom.example.ru/products/u-utp");
  });
});
