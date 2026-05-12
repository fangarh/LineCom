import { describe, expect, it } from "vitest";
import type { AdminProductImage } from "@/lib/api/admin-catalog";
import { formsFromProductImages, reorderProductImages } from "./admin-product-image-helpers";

const firstImage: AdminProductImage = {
  id: "image-first",
  storedFileId: "file-first",
  url: "/uploads/first.jpg",
  originalFileName: "first.jpg",
  contentType: "image/jpeg",
  sizeBytes: 1024,
  checksum: "checksum-first",
  alt: "Первое изображение",
  title: null,
  sortOrder: 10,
  isMain: true,
  createdAt: "2026-05-12T08:00:00Z",
};

const secondImage: AdminProductImage = {
  ...firstImage,
  id: "image-second",
  storedFileId: "file-second",
  originalFileName: "second.jpg",
  alt: "Второе изображение",
  title: "Второе",
  sortOrder: 20,
  isMain: false,
};

describe("admin product image helpers", () => {
  it("maps image metadata into editable form state", () => {
    expect(formsFromProductImages([firstImage, secondImage])).toEqual({
      "image-first": { alt: "Первое изображение", title: "" },
      "image-second": { alt: "Второе изображение", title: "Второе" },
    });
  });

  it("reorders images by direction without mutating the source array", () => {
    const images = [firstImage, secondImage];
    const reordered = reorderProductImages(images, "image-second", -1);

    expect(reordered.map((image) => image.id)).toEqual(["image-second", "image-first"]);
    expect(images.map((image) => image.id)).toEqual(["image-first", "image-second"]);
  });

  it("returns original order when target move is outside the list", () => {
    expect(reorderProductImages([firstImage, secondImage], "image-first", -1).map((image) => image.id)).toEqual([
      "image-first",
      "image-second",
    ]);
    expect(reorderProductImages([firstImage, secondImage], "missing", 1).map((image) => image.id)).toEqual([
      "image-first",
      "image-second",
    ]);
  });
});
