import { describe, expect, it } from "vitest";
import type {
  AdminCategoryAttribute,
  AdminProductAttributeValue,
  AdminProductDetail,
} from "@/lib/api/admin-catalog";
import {
  buildAdminProductAttributesCommand,
  buildAdminProductCommand,
  buildProductAttributeEditorState,
  formFromAdminProductDetail,
} from "./admin-product-editor-helpers";

const productDetail: AdminProductDetail = {
  id: "product-1",
  categoryId: "category-1",
  categoryName: "Кабели",
  brandId: "brand-1",
  brandName: "Завод",
  name: "Кабель ВВГнг",
  slug: "kabel-vvgng",
  sku: " SKU-1 ",
  externalId: null,
  description: "Описание",
  shortDescription: null,
  availabilityStatus: "in_stock",
  saleUnit: "м",
  unitQuantity: "1",
  publishStatus: "draft",
  isActive: true,
  seoTitle: "SEO title",
  seoDescription: null,
  h1: "H1",
  sortOrder: 12,
  readiness: { canPublish: false, issues: [] },
  images: { imagesCount: 0, mainImageFileId: null },
  attributes: [],
};

const textAttributeValue: AdminProductAttributeValue = {
  attributeId: "attr-color",
  code: "color",
  name: "Цвет",
  type: "text",
  unit: null,
  valueText: "Черный",
  valueNumber: null,
  valueBoolean: null,
  attributeOptionId: null,
  optionValue: null,
};

const categoryAttributes: AdminCategoryAttribute[] = [
  {
    id: "attr-color",
    categoryId: "category-1",
    name: "Цвет",
    code: "color",
    type: "text",
    unit: null,
    isRequired: false,
    isFilterable: true,
    isComparable: true,
    isVisibleInProduct: true,
    isSeoImportant: false,
    isUsedInGeneratedName: false,
    sortOrder: 10,
    isActive: true,
    productValuesCount: 3,
    options: [],
  },
  {
    id: "attr-length",
    categoryId: "category-1",
    name: "Длина",
    code: "length",
    type: "number",
    unit: "м",
    isRequired: false,
    isFilterable: true,
    isComparable: true,
    isVisibleInProduct: true,
    isSeoImportant: false,
    isUsedInGeneratedName: false,
    sortOrder: 20,
    isActive: true,
    productValuesCount: 1,
    options: [],
  },
  {
    id: "attr-kit",
    categoryId: "category-1",
    name: "Комплект",
    code: "kit",
    type: "boolean",
    unit: null,
    isRequired: false,
    isFilterable: false,
    isComparable: false,
    isVisibleInProduct: true,
    isSeoImportant: false,
    isUsedInGeneratedName: false,
    sortOrder: 30,
    isActive: true,
    productValuesCount: 0,
    options: [],
  },
  {
    id: "attr-material",
    categoryId: "category-1",
    name: "Материал",
    code: "material",
    type: "select",
    unit: null,
    isRequired: false,
    isFilterable: true,
    isComparable: true,
    isVisibleInProduct: true,
    isSeoImportant: true,
    isUsedInGeneratedName: false,
    sortOrder: 40,
    isActive: true,
    productValuesCount: 2,
    options: [
      {
        id: "option-copper",
        value: "Медь",
        slug: "copper",
        normalizedValue: "медь",
        sortOrder: 10,
        isActive: true,
        productValuesCount: 2,
      },
      {
        id: "option-inactive",
        value: "Серебро",
        slug: "silver",
        normalizedValue: "серебро",
        sortOrder: 20,
        isActive: false,
        productValuesCount: 0,
      },
    ],
  },
  {
    id: "attr-hidden",
    categoryId: "category-1",
    name: "Скрытая",
    code: "hidden",
    type: "text",
    unit: null,
    isRequired: false,
    isFilterable: false,
    isComparable: false,
    isVisibleInProduct: false,
    isSeoImportant: false,
    isUsedInGeneratedName: false,
    sortOrder: 50,
    isActive: false,
    productValuesCount: 0,
    options: [],
  },
];

describe("admin product editor helpers", () => {
  it("maps product detail into editable form state", () => {
    expect(formFromAdminProductDetail(productDetail)).toEqual({
      categoryId: "category-1",
      brandId: "brand-1",
      name: "Кабель ВВГнг",
      slug: "kabel-vvgng",
      sku: " SKU-1 ",
      externalId: "",
      description: "Описание",
      shortDescription: "",
      availabilityStatus: "in_stock",
      saleUnit: "м",
      unitQuantity: "1",
      sortOrder: "12",
      h1: "H1",
      seoTitle: "SEO title",
      seoDescription: "",
      publishStatus: "draft",
      isActive: true,
    });
  });

  it("builds product update command with trimmed optional fields and numeric sort order", () => {
    expect(
      buildAdminProductCommand({
        categoryId: "category-1",
        brandId: "",
        name: " Кабель ",
        slug: " kabel ",
        sku: " ",
        externalId: " ERP-1 ",
        description: "",
        shortDescription: " Кратко ",
        availabilityStatus: "preorder",
        saleUnit: " шт ",
        unitQuantity: "1",
        sortOrder: "not-a-number",
        h1: "",
        seoTitle: " SEO ",
        seoDescription: " ",
        publishStatus: "published",
        isActive: false,
      }),
    ).toEqual({
      categoryId: "category-1",
      brandId: null,
      name: "Кабель",
      slug: "kabel",
      sku: null,
      externalId: "ERP-1",
      description: null,
      shortDescription: "Кратко",
      availabilityStatus: "preorder",
      saleUnit: "шт",
      unitQuantity: "1",
      publishStatus: "published",
      isActive: false,
      seoTitle: "SEO",
      seoDescription: null,
      h1: null,
      sortOrder: 0,
    });
  });

  it("merges active category attributes with existing product values and active select options", () => {
    const state = buildProductAttributeEditorState(categoryAttributes, [textAttributeValue]);

    expect(state.rows.map((row) => row.attributeId)).toEqual([
      "attr-color",
      "attr-length",
      "attr-kit",
      "attr-material",
    ]);
    expect(state.rows.find((row) => row.attributeId === "attr-material")?.options).toEqual([
      expect.objectContaining({ id: "option-copper" }),
    ]);
    expect(state.values["attr-color"]).toEqual({
      valueText: "Черный",
      valueNumber: "",
      valueBoolean: false,
      attributeOptionId: "",
    });
  });

  it("builds attributes update payload and omits cleared non-boolean values", () => {
    const { rows } = buildProductAttributeEditorState(categoryAttributes, []);

    expect(
      buildAdminProductAttributesCommand(rows, {
        "attr-color": { valueText: " Синий ", valueNumber: "", valueBoolean: false, attributeOptionId: "" },
        "attr-length": { valueText: "", valueNumber: "", valueBoolean: false, attributeOptionId: "" },
        "attr-kit": { valueText: "", valueNumber: "", valueBoolean: false, attributeOptionId: "" },
        "attr-material": { valueText: "", valueNumber: "", valueBoolean: false, attributeOptionId: "option-copper" },
      }),
    ).toEqual({
      values: [
        { attributeId: "attr-color", valueText: "Синий" },
        { attributeId: "attr-kit", valueBoolean: false },
        { attributeId: "attr-material", attributeOptionId: "option-copper" },
      ],
    });
  });
});
