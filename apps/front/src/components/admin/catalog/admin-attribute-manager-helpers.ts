import type {
  AdminAttributeOption,
  AdminCategoryAttribute,
  UpsertAdminAttributeOptionCommand,
  UpsertAdminCategoryAttributeCommand,
} from "@/lib/api/admin-catalog";

export type AttributeFormState = {
  name: string;
  code: string;
  type: string;
  unit: string;
  isRequired: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isVisibleInProduct: boolean;
  isSeoImportant: boolean;
  isUsedInGeneratedName: boolean;
  sortOrder: string;
  isActive: boolean;
};

export type OptionFormState = {
  value: string;
  slug: string;
  normalizedValue: string;
  sortOrder: string;
  isActive: boolean;
};

export const emptyAttributeForm: AttributeFormState = {
  name: "",
  code: "",
  type: "text",
  unit: "",
  isRequired: false,
  isFilterable: false,
  isComparable: false,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: false,
  sortOrder: "0",
  isActive: true,
};

export const emptyOptionForm: OptionFormState = {
  value: "",
  slug: "",
  normalizedValue: "",
  sortOrder: "0",
  isActive: true,
};

export const attributeTypes = [
  { value: "text", label: "Текст" },
  { value: "number", label: "Число" },
  { value: "boolean", label: "Да/нет" },
  { value: "select", label: "Список" },
];

export function attributeFormFromDetail(attribute: AdminCategoryAttribute): AttributeFormState {
  return {
    name: attribute.name,
    code: attribute.code,
    type: attribute.type,
    unit: attribute.unit ?? "",
    isRequired: attribute.isRequired,
    isFilterable: attribute.isFilterable,
    isComparable: attribute.isComparable,
    isVisibleInProduct: attribute.isVisibleInProduct,
    isSeoImportant: attribute.isSeoImportant,
    isUsedInGeneratedName: attribute.isUsedInGeneratedName,
    sortOrder: String(attribute.sortOrder),
    isActive: attribute.isActive,
  };
}

export function optionFormFromDetail(option: AdminAttributeOption): OptionFormState {
  return {
    value: option.value,
    slug: option.slug,
    normalizedValue: option.normalizedValue,
    sortOrder: String(option.sortOrder),
    isActive: option.isActive,
  };
}

export function buildAttributeCommand(form: AttributeFormState): UpsertAdminCategoryAttributeCommand {
  return {
    name: form.name.trim(),
    code: form.code.trim(),
    type: form.type,
    unit: normalizeOptionalText(form.unit),
    isRequired: form.isRequired,
    isFilterable: form.isFilterable,
    isComparable: form.isComparable,
    isVisibleInProduct: form.isVisibleInProduct,
    isSeoImportant: form.isSeoImportant,
    isUsedInGeneratedName: form.isUsedInGeneratedName,
    sortOrder: parseSortOrder(form.sortOrder),
    isActive: form.isActive,
  };
}

export function buildOptionCommand(form: OptionFormState): UpsertAdminAttributeOptionCommand {
  return {
    value: form.value.trim(),
    slug: form.slug.trim(),
    normalizedValue: form.normalizedValue.trim(),
    sortOrder: parseSortOrder(form.sortOrder),
    isActive: form.isActive,
  };
}

export function mergeAttributeOptions(items: AdminCategoryAttribute[], savedAttribute: AdminCategoryAttribute) {
  if (savedAttribute.options.length) {
    return savedAttribute;
  }

  const existingAttribute = items.find((item) => item.id === savedAttribute.id);
  if (!existingAttribute?.options.length) {
    return savedAttribute;
  }

  return { ...savedAttribute, options: existingAttribute.options };
}

export function upsertAttribute(items: AdminCategoryAttribute[], attribute: AdminCategoryAttribute) {
  const existingIndex = items.findIndex((item) => item.id === attribute.id);
  if (existingIndex === -1) {
    return [...items, attribute].sort(compareBySortOrder);
  }

  return items.map((item) => (item.id === attribute.id ? attribute : item)).sort(compareBySortOrder);
}

export function upsertOption(items: AdminAttributeOption[], option: AdminAttributeOption) {
  const existingIndex = items.findIndex((item) => item.id === option.id);
  if (existingIndex === -1) {
    return [...items, option].sort(compareBySortOrder);
  }

  return items.map((item) => (item.id === option.id ? option : item)).sort(compareBySortOrder);
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}

function parseSortOrder(value: string) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function compareBySortOrder(left: { sortOrder: number }, right: { sortOrder: number }) {
  return left.sortOrder - right.sortOrder;
}
