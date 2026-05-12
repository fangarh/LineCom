import type {
  AdminAttributeOption,
  AdminCategoryAttribute,
  AdminProductAttributeValue,
  AdminProductDetail,
  UpdateAdminProductAttributesCommand,
  UpsertAdminProductAttributeValueCommand,
  UpsertAdminProductCommand,
} from "@/lib/api/admin-catalog";

export type ProductEditorTab = "main" | "attributes" | "images" | "seo" | "publication";

export type ProductFormState = {
  categoryId: string;
  brandId: string;
  name: string;
  slug: string;
  sku: string;
  externalId: string;
  description: string;
  shortDescription: string;
  availabilityStatus: string;
  saleUnit: string;
  unitQuantity: string;
  sortOrder: string;
  h1: string;
  seoTitle: string;
  seoDescription: string;
  publishStatus: string;
  isActive: boolean;
};

export type ProductAttributeFormState = {
  valueText: string;
  valueNumber: string;
  valueBoolean: boolean;
  attributeOptionId: string;
};

export type ProductAttributeEditorRow = {
  attributeId: string;
  name: string;
  type: string;
  unit: string | null;
  options: AdminAttributeOption[];
};

export const emptyProductForm: ProductFormState = {
  categoryId: "",
  brandId: "",
  name: "",
  slug: "",
  sku: "",
  externalId: "",
  description: "",
  shortDescription: "",
  availabilityStatus: "in_stock",
  saleUnit: "шт",
  unitQuantity: "1",
  sortOrder: "0",
  h1: "",
  seoTitle: "",
  seoDescription: "",
  publishStatus: "draft",
  isActive: true,
};

export const productEditorTabs: { id: ProductEditorTab; label: string }[] = [
  { id: "main", label: "Основное" },
  { id: "attributes", label: "Характеристики" },
  { id: "images", label: "Изображения" },
  { id: "seo", label: "SEO" },
  { id: "publication", label: "Публикация" },
];

export function formFromAdminProductDetail(product: AdminProductDetail): ProductFormState {
  return {
    categoryId: product.categoryId,
    brandId: product.brandId ?? "",
    name: product.name,
    slug: product.slug,
    sku: product.sku ?? "",
    externalId: product.externalId ?? "",
    description: product.description ?? "",
    shortDescription: product.shortDescription ?? "",
    availabilityStatus: product.availabilityStatus,
    saleUnit: product.saleUnit,
    unitQuantity: product.unitQuantity,
    sortOrder: String(product.sortOrder),
    h1: product.h1 ?? "",
    seoTitle: product.seoTitle ?? "",
    seoDescription: product.seoDescription ?? "",
    publishStatus: product.publishStatus,
    isActive: product.isActive,
  };
}

export function buildAdminProductCommand(form: ProductFormState): UpsertAdminProductCommand {
  return {
    categoryId: form.categoryId || null,
    brandId: form.brandId || null,
    name: form.name.trim(),
    slug: form.slug.trim(),
    sku: normalizeOptionalText(form.sku),
    externalId: normalizeOptionalText(form.externalId),
    description: normalizeOptionalText(form.description),
    shortDescription: normalizeOptionalText(form.shortDescription),
    availabilityStatus: form.availabilityStatus,
    saleUnit: form.saleUnit.trim(),
    unitQuantity: form.unitQuantity.trim(),
    publishStatus: form.publishStatus,
    isActive: form.isActive,
    seoTitle: normalizeOptionalText(form.seoTitle),
    seoDescription: normalizeOptionalText(form.seoDescription),
    h1: normalizeOptionalText(form.h1),
    sortOrder: parseSortOrder(form.sortOrder),
  };
}

export function buildProductAttributeEditorState(
  categoryAttributes: AdminCategoryAttribute[],
  productAttributeValues: AdminProductAttributeValue[],
) {
  return {
    rows: buildProductAttributeEditorRows(categoryAttributes),
    values: valuesFromProductAttributeRows(productAttributeValues),
  };
}

export function buildProductAttributeEditorRows(categoryAttributes: AdminCategoryAttribute[]) {
  return categoryAttributes
    .filter((attribute) => attribute.isActive)
    .map<ProductAttributeEditorRow>((attribute) => ({
      attributeId: attribute.id,
      name: attribute.name,
      type: attribute.type,
      unit: attribute.unit,
      options: attribute.options.filter((option) => option.isActive),
    }));
}

export function valuesFromProductAttributeRows(rows: AdminProductAttributeValue[]) {
  return rows.reduce<Record<string, ProductAttributeFormState>>((values, row) => {
    values[row.attributeId] = valueFromProductAttributeValue(row);
    return values;
  }, {});
}

export function valueFromProductAttributeValue(row: AdminProductAttributeValue): ProductAttributeFormState {
  return {
    valueText: row.valueText ?? "",
    valueNumber: row.valueNumber === null ? "" : String(row.valueNumber),
    valueBoolean: row.valueBoolean ?? false,
    attributeOptionId: row.attributeOptionId ?? "",
  };
}

export function emptyProductAttributeValue(): ProductAttributeFormState {
  return {
    valueText: "",
    valueNumber: "",
    valueBoolean: false,
    attributeOptionId: "",
  };
}

export function buildAdminProductAttributesCommand(
  rows: ProductAttributeEditorRow[],
  values: Record<string, ProductAttributeFormState>,
): UpdateAdminProductAttributesCommand {
  return {
    values: rows
      .map((attribute) => commandFromProductAttribute(attribute, values[attribute.attributeId]))
      .filter((value): value is UpsertAdminProductAttributeValueCommand => value !== null),
  };
}

export function commandFromProductAttribute(
  row: ProductAttributeEditorRow,
  value: ProductAttributeFormState | undefined,
): UpsertAdminProductAttributeValueCommand | null {
  const currentValue = value ?? emptyProductAttributeValue();

  if (row.type === "number") {
    const valueNumber = parseNullableNumber(currentValue.valueNumber);
    if (valueNumber === null) return null;

    return {
      attributeId: row.attributeId,
      valueNumber,
    };
  }

  if (row.type === "boolean") {
    return {
      attributeId: row.attributeId,
      valueBoolean: currentValue.valueBoolean,
    };
  }

  if (row.type === "select") {
    if (!currentValue.attributeOptionId) return null;

    return {
      attributeId: row.attributeId,
      attributeOptionId: currentValue.attributeOptionId,
    };
  }

  const valueText = normalizeOptionalText(currentValue.valueText);
  if (valueText === null) return null;

  return {
    attributeId: row.attributeId,
    valueText,
  };
}

export function parseNullableNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

export function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}

export function parseSortOrder(value: string) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function getProductEditorTabId(tab: ProductEditorTab) {
  return `admin-product-${tab}-tab`;
}

export function getProductEditorPanelId(tab: ProductEditorTab) {
  return `admin-product-${tab}-panel`;
}
