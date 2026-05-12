import type { AdminBrandListItem, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import type { ProductFormState } from "./admin-product-editor-helpers";

type AdminProductMainFieldsProps = {
  brands: AdminBrandListItem[];
  categories: AdminCategoryListItem[];
  form: ProductFormState;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
};

export function AdminProductMainFields({ brands, categories, form, setForm }: AdminProductMainFieldsProps) {
  return (
    <div className="admin-product-form__grid">
      <label className="form-field">
        <span>Категория</span>
        <select onChange={(event) => setForm((current) => ({ ...current, categoryId: event.target.value }))} required value={form.categoryId}>
          <option value="">Выберите категорию</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </label>
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
        <input onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required value={form.name} />
      </label>
      <label className="form-field">
        <span>Slug</span>
        <input onChange={(event) => setForm((current) => ({ ...current, slug: event.target.value }))} required value={form.slug} />
      </label>
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
