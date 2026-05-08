import type { ProductListParams, PublicFilter } from "@/lib/api/catalog";

export const SORT_OPTIONS = [
  { value: "category", label: "По категории" },
  { value: "name", label: "По названию" },
  { value: "newest", label: "Сначала новые" },
] as const;

export const AVAILABILITY_FILTER_OPTIONS = [
  { value: "in_stock", label: "В наличии" },
  { value: "on_order", label: "Под заказ" },
  { value: "check_availability", label: "Уточнить" },
] as const;

export const SALE_UNIT_FILTER_OPTIONS = [
  { value: "coil", label: "Бухта" },
  { value: "box", label: "Коробка" },
  { value: "piece", label: "Штука" },
  { value: "pack", label: "Упаковка" },
] as const;

export type CatalogSort = (typeof SORT_OPTIONS)[number]["value"];

export type CatalogSearchParams = Record<string, string | string[] | undefined>;

export type CatalogFilterState = {
  sort: CatalogSort;
  availabilityStatus?: string;
  saleUnit?: string;
  attributes: Record<string, string>;
};

const DEFAULT_SORT: CatalogSort = "category";
const SORT_VALUES = new Set<string>(SORT_OPTIONS.map((option) => option.value));
const AVAILABILITY_VALUES = new Set<string>(AVAILABILITY_FILTER_OPTIONS.map((option) => option.value));
const SALE_UNIT_VALUES = new Set<string>(SALE_UNIT_FILTER_OPTIONS.map((option) => option.value));

export function parseCatalogFilters(
  searchParams: CatalogSearchParams = {},
  attributeFilters: PublicFilter[] = [],
): CatalogFilterState {
  const sort = firstParamValue(searchParams.sort);
  const availabilityStatus = firstParamValue(searchParams.availabilityStatus);
  const saleUnit = firstParamValue(searchParams.saleUnit);

  return {
    sort: sort && SORT_VALUES.has(sort) ? (sort as CatalogSort) : DEFAULT_SORT,
    availabilityStatus: availabilityStatus && AVAILABILITY_VALUES.has(availabilityStatus) ? availabilityStatus : undefined,
    saleUnit: saleUnit && SALE_UNIT_VALUES.has(saleUnit) ? saleUnit : undefined,
    attributes: parseAttributeFilters(searchParams, attributeFilters),
  };
}

export function toProductListParams(filters: CatalogFilterState, categorySlug?: string): ProductListParams {
  return {
    categorySlug,
    pageSize: 24,
    sort: filters.sort,
    availabilityStatus: filters.availabilityStatus,
    saleUnit: filters.saleUnit,
    attributes: filters.attributes,
  };
}

export function countActiveFilters(filters: CatalogFilterState): number {
  return Number(Boolean(filters.availabilityStatus)) + Number(Boolean(filters.saleUnit)) + Object.keys(filters.attributes).length;
}

function parseAttributeFilters(searchParams: CatalogSearchParams, attributeFilters: PublicFilter[]): Record<string, string> {
  const allowedOptions = new Map<string, Set<string>>();

  for (const filter of attributeFilters) {
    allowedOptions.set(
      filter.code,
      new Set(filter.options.map((option) => option.slug)),
    );
  }

  const attributes: Record<string, string> = {};

  for (const [key, rawValue] of Object.entries(searchParams)) {
    if (!key.startsWith("attribute.")) {
      continue;
    }

    const code = key.slice("attribute.".length);
    const value = firstParamValue(rawValue);
    const allowedValues = allowedOptions.get(code);

    if (value && allowedValues?.has(value)) {
      attributes[code] = value;
    }
  }

  return attributes;
}

function firstParamValue(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
