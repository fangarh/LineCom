import type { AdminProductDetail } from "@/lib/api/admin-catalog";
import type { ProductFormState } from "./admin-product-editor-helpers";

type AdminProductPublicationFieldsProps = {
  form: ProductFormState;
  selectedProduct: AdminProductDetail | null;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
};

export function AdminProductPublicationFields({ form, selectedProduct, setForm }: AdminProductPublicationFieldsProps) {
  const canPublish = selectedProduct?.readiness.canPublish ?? false;
  const issues = selectedProduct?.readiness.issues ?? [];

  return (
    <div className="admin-product-form__grid">
      <label className="form-field">
        <span>Статус публикации</span>
        <select onChange={(event) => setForm((current) => ({ ...current, publishStatus: event.target.value }))} value={form.publishStatus}>
          <option value="draft">Черновик</option>
          <option value="review">Проверка</option>
          <option value="published">Опубликован</option>
          <option value="archived">Архив</option>
        </select>
      </label>
      <label className="admin-product-manager__check">
        <input
          checked={form.isActive}
          onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
          type="checkbox"
        />
        <span>Активен</span>
      </label>
      <div className="admin-product-manager__readiness">
        <strong>{canPublish ? "Можно опубликовать" : "Нельзя опубликовать"}</strong>
        {issues.length ? (
          <ul>
            {issues.map((issue) => (
              <li key={issue.code}>{issue.message}</li>
            ))}
          </ul>
        ) : (
          <p className="admin-catalog-status">Проблем готовности нет.</p>
        )}
      </div>
    </div>
  );
}
