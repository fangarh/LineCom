import type { AdminBrandListItem, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import type { ProductFormState } from "./admin-product-editor-helpers";
import { AdminCategoryTreePicker } from "./admin-category-parent-picker";

type AdminProductMainFieldsProps = {
  brands: AdminBrandListItem[];
  categories: AdminCategoryListItem[];
  form: ProductFormState;
  onNameChange: (name: string) => void;
  onRegenerateSlug: () => void;
  onSlugChange: (slug: string) => void;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
};

export function AdminProductMainFields({
  brands,
  categories,
  form,
  onNameChange,
  onRegenerateSlug,
  onSlugChange,
  setForm,
}: AdminProductMainFieldsProps) {
  return (
    <div className="admin-product-form__grid">
      <AdminCategoryTreePicker
        buttonLabel="Выбрать категорию"
        categories={categories}
        emptySelection={{
          ariaLabel: "Выбрать категорию",
          title: "Выберите категорию",
          description: "Только конечная категория без подкатегорий",
        }}
        getDisabledReason={() => "выберите подкатегорию"}
        isCategoryDisabled={(node) => node.hasChildren || node.category.childrenCount > 0}
        label="Категория"
        onChange={(categoryId) => setForm((current) => ({ ...current, categoryId }))}
        value={form.categoryId}
      />
      <label className="form-field">
        <span>Бренд</span>
        <select onChange={(event) => setForm((current) => ({ ...current, brandId: event.target.value }))} value={form.brandId}>
          <option value="">Без бренда</option>
          {brands.map((brand) => (
            <option key={brand.id} value={brand.id}>
              {brand.name}
            </option>
          ))}
        </select>
      </label>
      <label className="form-field">
        <span>Название</span>
        <input onChange={(event) => onNameChange(event.target.value)} required value={form.name} />
      </label>
      <label className="form-field">
        <span>Slug</span>
        <input onChange={(event) => onSlugChange(event.target.value)} onFocus={(event) => event.currentTarget.select()} required value={form.slug} />
      </label>
      <button className="button button--ghost" onClick={onRegenerateSlug} type="button">
        Сгенерировать заново
      </button>
      <label className="form-field">
        <span>SKU</span>
        <input onChange={(event) => setForm((current) => ({ ...current, sku: event.target.value }))} value={form.sku} />
      </label>
      <label className="form-field">
        <span>External ID</span>
        <input onChange={(event) => setForm((current) => ({ ...current, externalId: event.target.value }))} value={form.externalId} />
      </label>
      <label className="form-field">
        <span>Краткое описание</span>
        <textarea
          onChange={(event) => setForm((current) => ({ ...current, shortDescription: event.target.value }))}
          rows={3}
          value={form.shortDescription}
        />
      </label>
      <label className="form-field">
        <span>Описание</span>
        <textarea onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} value={form.description} />
      </label>
      <label className="form-field">
        <span>Наличие</span>
        <select onChange={(event) => setForm((current) => ({ ...current, availabilityStatus: event.target.value }))} value={form.availabilityStatus}>
          <option value="in_stock">В наличии</option>
          <option value="preorder">Под заказ</option>
          <option value="out_of_stock">Нет в наличии</option>
        </select>
      </label>
      <label className="form-field">
        <span>Единица продажи</span>
        <input onChange={(event) => setForm((current) => ({ ...current, saleUnit: event.target.value }))} required value={form.saleUnit} />
      </label>
      <label className="form-field">
        <span>Количество в единице</span>
        <input onChange={(event) => setForm((current) => ({ ...current, unitQuantity: event.target.value }))} required value={form.unitQuantity} />
      </label>
      <label className="form-field">
        <span>Сортировка</span>
        <input
          inputMode="numeric"
          onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))}
          type="number"
          value={form.sortOrder}
        />
      </label>
    </div>
  );
}
