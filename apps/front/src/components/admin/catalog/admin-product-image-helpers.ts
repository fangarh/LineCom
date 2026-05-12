import type { AdminProductImage } from "@/lib/api/admin-catalog";

export type ImageFormState = {
  alt: string;
  title: string;
};

export function formsFromProductImages(images: AdminProductImage[]) {
  return images.reduce<Record<string, ImageFormState>>((forms, image) => {
    forms[image.id] = {
      alt: image.alt,
      title: image.title ?? "",
    };
    return forms;
  }, {});
}

export function reorderProductImages(images: AdminProductImage[], imageId: string, direction: -1 | 1) {
  const index = images.findIndex((image) => image.id === imageId);
  const targetIndex = index + direction;
  if (index < 0 || targetIndex < 0 || targetIndex >= images.length) return images;

  const orderedImages = [...images];
  [orderedImages[index], orderedImages[targetIndex]] = [orderedImages[targetIndex], orderedImages[index]];
  return orderedImages;
}

export function normalizeOptionalImageText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}
