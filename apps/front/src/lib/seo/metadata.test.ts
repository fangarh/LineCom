import { describe, expect, it } from "vitest";
import { indexablePageMetadata, noindexPageMetadata } from "./metadata";

describe("SEO metadata helpers", () => {
  it("creates canonical metadata for indexable public pages", () => {
    expect(indexablePageMetadata({
      title: "Каталог LineCom",
      description: "Кабель и компоненты.",
      canonicalPath: "/catalog",
    })).toEqual({
      title: "Каталог LineCom",
      description: "Кабель и компоненты.",
      alternates: {
        canonical: "/catalog",
      },
      robots: {
        index: true,
        follow: true,
        googleBot: {
          index: true,
          follow: true,
          "max-image-preview": "large",
          "max-snippet": -1,
          "max-video-preview": -1,
        },
      },
    });
  });

  it("omits empty optional descriptions", () => {
    expect(indexablePageMetadata({
      title: "LineCom",
      canonicalPath: "/",
    }).description).toBeUndefined();
  });

  it("creates noindex metadata for internal and unavailable pages", () => {
    expect(noindexPageMetadata("Админка LineCom")).toEqual({
      title: "Админка LineCom",
      robots: {
        index: false,
        follow: false,
      },
    });
  });
});
