import Link from "next/link";
import type { PublicFilter } from "@/lib/api/catalog";
import {
  AVAILABILITY_FILTER_OPTIONS,
  SALE_UNIT_FILTER_OPTIONS,
  SORT_OPTIONS,
  countActiveFilters,
  type CatalogFilterState,
  type CatalogSort,
} from "@/lib/catalog/filtering";

type FilterOption = {
  value: string;
  label: string;
};

type CatalogFiltersProps = {
  attributeFilters?: PublicFilter[];
  basePath: string;
  state: CatalogFilterState;
  scopeLabel: string;
  totalItems?: number;
};

export function CatalogFilters({ attributeFilters = [], basePath, state, scopeLabel, totalItems }: CatalogFiltersProps) {
  const activeCount = countActiveFilters(state);

  return (
    <section className="catalog-filters" aria-labelledby="catalog-filters-title">
      <div className="catalog-filters__head">
        <div>
          <p className="eyebrow">Подбор</p>
          <h2 id="catalog-filters-title">Фильтры товаров</h2>
        </div>
        <div className="catalog-filters__summary">
          <span>{scopeLabel}</span>
          {typeof totalItems === "number" ? <strong>{totalItems} позиций</strong> : null}
        </div>
      </div>

      <FilterGroup
        title="Наличие"
        options={AVAILABILITY_FILTER_OPTIONS}
        activeValue={state.availabilityStatus}
        hrefFor={(value) => buildFilterHref(basePath, state, { availabilityStatus: value })}
      />

      <FilterGroup
        title="Единица продажи"
        options={SALE_UNIT_FILTER_OPTIONS}
        activeValue={state.saleUnit}
        hrefFor={(value) => buildFilterHref(basePath, state, { saleUnit: value })}
      />

      {attributeFilters.map((filter) => (
        <FilterGroup
          key={filter.code}
          title={filter.unit ? `${filter.name}, ${filter.unit}` : filter.name}
          options={filter.options.map((option) => ({ value: option.slug, label: option.value }))}
          activeValue={state.attributes[filter.code]}
          hrefFor={(value) => buildFilterHref(basePath, state, { attribute: { code: filter.code, value } })}
        />
      ))}

      <FilterGroup
        title="Сортировка"
        options={SORT_OPTIONS}
        activeValue={state.sort}
        hrefFor={(value) => buildFilterHref(basePath, state, { sort: value as CatalogSort })}
      />

      {activeCount > 0 ? (
        <Link className="catalog-filters__reset" href={basePath}>
          Сбросить фильтры
        </Link>
      ) : null}
    </section>
  );
}

function FilterGroup({
  activeValue,
  hrefFor,
  options,
  title,
}: {
  activeValue?: string;
  hrefFor: (value: string) => string;
  options: readonly FilterOption[];
  title: string;
}) {
  return (
    <div className="catalog-filter-group">
      <h3>{title}</h3>
      <div className="filter-chip-list">
        {options.map((option) => {
          const isActive = option.value === activeValue;

          return (
            <Link
              key={option.value}
              className={isActive ? "filter-chip filter-chip--active" : "filter-chip"}
              href={hrefFor(option.value)}
              aria-current={isActive ? "true" : undefined}
            >
              {option.label}
            </Link>
          );
        })}
      </div>
    </div>
  );
}

function buildFilterHref(basePath: string, state: CatalogFilterState, patch: FilterPatch): string {
  const nextState: CatalogFilterState = {
    sort: state.sort,
    availabilityStatus: state.availabilityStatus,
    saleUnit: state.saleUnit,
    attributes: { ...state.attributes },
  };

  if ("availabilityStatus" in patch) {
    nextState.availabilityStatus = state.availabilityStatus === patch.availabilityStatus ? undefined : patch.availabilityStatus;
  }

  if ("saleUnit" in patch) {
    nextState.saleUnit = state.saleUnit === patch.saleUnit ? undefined : patch.saleUnit;
  }

  if ("sort" in patch) {
    nextState.sort = patch.sort;
  }

  if ("attribute" in patch) {
    const { code, value } = patch.attribute;

    if (state.attributes[code] === value) {
      delete nextState.attributes[code];
    } else {
      nextState.attributes[code] = value;
    }
  }

  const search = new URLSearchParams();

  if (nextState.sort !== "category") {
    search.set("sort", nextState.sort);
  }
  if (nextState.availabilityStatus) {
    search.set("availabilityStatus", nextState.availabilityStatus);
  }
  if (nextState.saleUnit) {
    search.set("saleUnit", nextState.saleUnit);
  }
  for (const [code, value] of Object.entries(nextState.attributes).sort(([left], [right]) => left.localeCompare(right))) {
    search.set(`attribute.${code}`, value);
  }

  const query = search.toString();
  return query ? `${basePath}?${query}` : basePath;
}

type FilterPatch =
  | { availabilityStatus: string }
  | { saleUnit: string }
  | { sort: CatalogSort }
  | { attribute: { code: string; value: string } };
