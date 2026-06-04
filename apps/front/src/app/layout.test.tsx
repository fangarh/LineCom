import { renderToStaticMarkup } from "react-dom/server";
import { afterEach, describe, expect, it, vi } from "vitest";
import RootLayout from "./layout";

const originalOrigin = process.env.LINECOM_PUBLIC_SITE_ORIGIN;

afterEach(() => {
  if (originalOrigin === undefined) {
    delete process.env.LINECOM_PUBLIC_SITE_ORIGIN;
    return;
  }

  process.env.LINECOM_PUBLIC_SITE_ORIGIN = originalOrigin;
});

vi.mock("@/components/auth/auth-provider", () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

vi.mock("@/components/request/request-draft-provider", () => ({
  RequestDraftProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

vi.mock("@/components/layout/site-shell", () => ({
  SiteShell: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

describe("root layout JSON-LD", () => {
  it("renders Organization and LocalBusiness facts for AI search extraction", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/";

    const html = renderToStaticMarkup(
      <RootLayout>
        <div>page</div>
      </RootLayout>,
    );
    const scripts = [...html.matchAll(/<script type="application\/ld\+json">([^<]+)<\/script>/g)].map((match) =>
      JSON.parse(match[1]),
    );

    expect(scripts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          "@context": "https://schema.org",
          "@type": ["Organization", "LocalBusiness"],
          name: "LineCom",
          legalName: "ООО «ЛАЙНКОМ»",
          url: "https://linecom.example.ru/",
          taxID: "7801724840",
          email: "al@line-com.ru",
          telephone: "+79313064350",
          address: expect.objectContaining({
            "@type": "PostalAddress",
            addressLocality: "Санкт-Петербург",
          }),
        }),
      ]),
    );
  });
});
