import type { ProductFormState } from "./admin-product-editor-helpers";

type AdminProductSeoFieldsProps = {
  form: ProductFormState;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
};

export function AdminProductSeoFields({ form, setForm }: AdminProductSeoFieldsProps) {
  return (
    <div className="admin-product-form__grid">
      <label className="form-field">
        <span>H1</span>
        <input onChange={(event) => setForm((current) => ({ ...current, h1: event.target.value }))} value={form.h1} />
      </label>
      <label className="form-field">
        <span>SEO title</span>
        <input onChange={(event) => setForm((current) => ({ ...current, seoTitle: event.target.value }))} value={form.seoTitle} />
      </label>
      <label className="form-field">
        <span>SEO description</span>
        <textarea
          onChange={(event) => setForm((current) => ({ ...current, seoDescription: event.target.value }))}
          rows={3}
          value={form.seoDescription}
        />
      </label>
    </div>
  );
}
