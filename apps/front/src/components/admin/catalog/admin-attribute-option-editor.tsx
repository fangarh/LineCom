import type { FormEvent } from "react";
import type { AdminAttributeOption, AdminCategoryAttribute } from "@/lib/api/admin-catalog";
import type { OptionFormState } from "./admin-attribute-manager-helpers";

type AdminAttributeOptionEditorProps = {
  isMutatingOption: boolean;
  onDeleteOption: () => void;
  onFormPatch: (patch: Partial<OptionFormState>) => void;
  onOptionSlugChange: (slug: string) => void;
  onOptionValueChange: (value: string) => void;
  onRegenerateOptionSlug: () => void;
  onSelectOption: (option: AdminAttributeOption) => void;
  onStartCreateOption: () => void;
  onSubmitOption: (event: FormEvent<HTMLFormElement>) => void;
  optionForm: OptionFormState;
  selectedAttribute: AdminCategoryAttribute;
  selectedOption: AdminAttributeOption | null;
};

export function AdminAttributeOptionEditor({
  isMutatingOption,
  onDeleteOption,
  onFormPatch,
  onOptionSlugChange,
  onOptionValueChange,
  onRegenerateOptionSlug,
  onSelectOption,
  onStartCreateOption,
  onSubmitOption,
  optionForm,
  selectedAttribute,
  selectedOption,
}: AdminAttributeOptionEditorProps) {
  return (
    <section className="admin-attribute-manager__options" aria-label="Редактор значения">
      <div className="admin-attribute-manager__head">
        <h2>Значения</h2>
        <button className="button button--secondary" onClick={onStartCreateOption} type="button">
          Новое значение
        </button>
      </div>

      <div className="admin-attribute-manager__option-rows">
        {selectedAttribute.options.length ? (
          selectedAttribute.options.map((option) => (
            <button
              aria-pressed={selectedOption?.id === option.id}
              className="admin-attribute-option-row"
              key={option.id}
              onClick={() => onSelectOption(option)}
              type="button"
            >
              <span>
                <strong>{option.value}</strong>
                <small>{option.slug}</small>
              </span>
              <span className="admin-attribute-row__meta">
                {option.productValuesCount} значений в товарах · {option.sortOrder}
              </span>
            </button>
          ))
        ) : (
          <p className="empty-state">Значения не найдены.</p>
        )}
      </div>

      <form className="admin-attribute-option-form" onSubmit={onSubmitOption}>
        <label className="form-field">
          <span>Значение</span>
          <input onChange={(event) => onOptionValueChange(event.target.value)} required value={optionForm.value} />
        </label>
        <label className="form-field">
          <span>Slug</span>
          <input
            onChange={(event) => onOptionSlugChange(event.target.value)}
            onFocus={(event) => event.currentTarget.select()}
            required
            value={optionForm.slug}
          />
        </label>
        <button className="button button--ghost" onClick={onRegenerateOptionSlug} type="button">
          Сгенерировать заново
        </button>
        <label className="form-field">
          <span>Нормализованное значение</span>
          <input
            onChange={(event) => onFormPatch({ normalizedValue: event.target.value })}
            required
            value={optionForm.normalizedValue}
          />
        </label>
        <label className="form-field">
          <span>Сортировка значения</span>
          <input
            inputMode="numeric"
            onChange={(event) => onFormPatch({ sortOrder: event.target.value })}
            type="number"
            value={optionForm.sortOrder}
          />
        </label>
        <label className="admin-attribute-manager__check">
          <input
            checked={optionForm.isActive}
            onChange={(event) => onFormPatch({ isActive: event.target.checked })}
            type="checkbox"
          />
          <span>Активно</span>
        </label>

        <div className="admin-attribute-manager__actions">
          <button className="button button--primary" disabled={isMutatingOption} type="submit">
            {selectedOption ? "Сохранить значение" : "Создать значение"}
          </button>
          <button
            className="button button--ghost"
            disabled={!selectedOption || isMutatingOption}
            onClick={onDeleteOption}
            type="button"
          >
            Удалить значение
          </button>
        </div>
      </form>
    </section>
  );
}
