import { describe, expect, it } from "vitest";
import type { AdminAttributeOption, AdminCategoryAttribute } from "@/lib/api/admin-catalog";
import {
  attributeFormFromDetail,
  buildAttributeCommand,
  buildOptionCommand,
  emptyAttributeForm,
  emptyOptionForm,
  mergeAttributeOptions,
  optionFormFromDetail,
  upsertAttribute,
  upsertOption,
} from "./admin-attribute-manager-helpers";

const redOption: AdminAttributeOption = {
  id: "option-red",
  value: "Красный",
  slug: "krasnyy",
  normalizedValue: "red",
  sortOrder: 20,
  isActive: true,
  productValuesCount: 4,
};

const blackOption: AdminAttributeOption = {
  id: "option-black",
  value: "Черный",
  slug: "chernyy",
  normalizedValue: "black",
  sortOrder: 10,
  isActive: false,
  productValuesCount: 0,
};

const colorAttribute: AdminCategoryAttribute = {
  id: "attr-color",
  categoryId: "cat-cables",
  name: "Цвет",
  code: "color",
  type: "select",
  unit: null,
  isRequired: true,
  isFilterable: true,
  isComparable: false,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: true,
  sortOrder: 20,
  isActive: true,
  productValuesCount: 3,
  options: [redOption],
};

const lengthAttribute: AdminCategoryAttribute = {
  id: "attr-length",
  categoryId: "cat-cables",
  name: "Длина",
  code: "length",
  type: "number",
  unit: "м",
  isRequired: false,
  isFilterable: true,
  isComparable: true,
  isVisibleInProduct: true,
  isSeoImportant: true,
  isUsedInGeneratedName: false,
  sortOrder: 10,
  isActive: true,
  productValuesCount: 0,
  options: [],
};

describe("admin attribute manager helpers", () => {
  it("maps attribute details to form state and trims command payloads", () => {
    expect(attributeFormFromDetail(lengthAttribute)).toEqual({
      name: "Длина",
      code: "length",
      type: "number",
      unit: "м",
      isRequired: false,
      isFilterable: true,
      isComparable: true,
      isVisibleInProduct: true,
      isSeoImportant: true,
      isUsedInGeneratedName: false,
      sortOrder: "10",
      isActive: true,
    });

    expect(
      buildAttributeCommand({
        ...emptyAttributeForm,
        name: "  Напряжение  ",
        code: " voltage ",
        unit: "   ",
        sortOrder: "not-number",
        isComparable: true,
      }),
    ).toEqual({
      name: "Напряжение",
      code: "voltage",
      type: "text",
      unit: null,
      isRequired: false,
      isFilterable: false,
      isComparable: true,
      isVisibleInProduct: true,
      isSeoImportant: false,
      isUsedInGeneratedName: false,
      sortOrder: 0,
      isActive: true,
    });
  });

  it("maps option details to form state and trims option payloads", () => {
    expect(optionFormFromDetail(redOption)).toEqual({
      value: "Красный",
      slug: "krasnyy",
      normalizedValue: "red",
      sortOrder: "20",
      isActive: true,
    });

    expect(
      buildOptionCommand({
        ...emptyOptionForm,
        value: "  Синий  ",
        slug: " siniy ",
        normalizedValue: " blue ",
        sortOrder: "",
        isActive: false,
      }),
    ).toEqual({
      value: "Синий",
      slug: "siniy",
      normalizedValue: "blue",
      sortOrder: 0,
      isActive: false,
    });
  });

  it("keeps existing select options when saved attribute omits them", () => {
    const savedAttribute = { ...colorAttribute, name: "Цвет оболочки", options: [] };

    expect(mergeAttributeOptions([colorAttribute], savedAttribute)).toEqual({
      ...savedAttribute,
      options: [redOption],
    });
  });

  it("upserts attributes and options by sort order", () => {
    const createdAttribute = { ...colorAttribute, id: "attr-voltage", name: "Напряжение", sortOrder: 5, options: [] };
    const updatedOption = { ...redOption, value: "Красный RAL", sortOrder: 30 };

    expect(upsertAttribute([colorAttribute, lengthAttribute], createdAttribute).map((attribute) => attribute.id)).toEqual([
      "attr-voltage",
      "attr-length",
      "attr-color",
    ]);
    expect(upsertAttribute([colorAttribute, lengthAttribute], { ...colorAttribute, sortOrder: 1 }).map((attribute) => attribute.id)).toEqual([
      "attr-color",
      "attr-length",
    ]);
    expect(upsertOption([redOption, blackOption], updatedOption)).toEqual([blackOption, updatedOption]);
  });
});
