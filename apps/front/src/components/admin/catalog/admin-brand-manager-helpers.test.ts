import { describe, expect, it } from "vitest";
import type { AdminBrandDetail } from "@/lib/api/admin-catalog";
import {
  brandFormFromDetail,
  buildBrandCommand,
  buildBrandListParams,
  emptyBrandForm,
  logoPreviewFromUpload,
} from "./admin-brand-manager-helpers";

const brandDetail: AdminBrandDetail = {
  id: "brand-cable",
  name: "Кабельный завод",
  slug: "kabelnyy-zavod",
  description: null,
  seoTitle: "Кабельный завод SEO",
  seoDescription: null,
  isActive: true,
  productsCount: 7,
  logoFileId: "logo-file",
};

describe("admin brand manager helpers", () => {
  it("maps brand details to form state and trims command payloads", () => {
    expect(brandFormFromDetail(brandDetail)).toEqual({
      name: "Кабельный завод",
      slug: "kabelnyy-zavod",
      description: "",
      seoTitle: "Кабельный завод SEO",
      seoDescription: "",
      isActive: true,
    });

    expect(
      buildBrandCommand({
        ...emptyBrandForm,
        name: "  ЭлектроКомплект  ",
        slug: " elektrokomplekt ",
        description: "   ",
        seoTitle: " ЭлектроКомплект SEO ",
        seoDescription: " SEO описание ",
        isActive: false,
      }),
    ).toEqual({
      name: "ЭлектроКомплект",
      slug: "elektrokomplekt",
      description: null,
      seoTitle: "ЭлектроКомплект SEO",
      seoDescription: "SEO описание",
      isActive: false,
    });
  });

  it("builds list params from trimmed search and active filter", () => {
    expect(buildBrandListParams("  кабель  ", "true")).toEqual({
      search: "кабель",
      isActive: true,
    });
    expect(buildBrandListParams("  ", "false")).toEqual({ isActive: false });
    expect(buildBrandListParams("", "")).toEqual({});
  });

  it("maps uploaded logo to preview state", () => {
    expect(
      logoPreviewFromUpload({
        storedFileId: "stored-logo",
        url: "/files/brands/logo.png",
        originalFileName: "logo.png",
        contentType: "image/png",
        sizeBytes: 1024,
        checksum: "checksum",
      }),
    ).toEqual({
      url: "/files/brands/logo.png",
      originalFileName: "logo.png",
    });
  });
});
