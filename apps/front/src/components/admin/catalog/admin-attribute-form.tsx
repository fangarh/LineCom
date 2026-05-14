import type { FormEvent } from "react";
import { attributeTypes, type AttributeFormState } from "./admin-attribute-manager-helpers";

type AdminAttributeFormProps = {
  attributeForm: AttributeFormState;
  isMutatingAttribute: boolean;
  isSelectedAttributeAvailable: boolean;
  isSelectedCategoryAvailable: boolean;
  onDeleteAttribute: () => void;
  onFormPatch: (patch: Partial<AttributeFormState>) => void;
  onSubmitAttribute: (event: FormEvent<HTMLFormElement>) => void;
  onTypeChange: (type: string) => void;
};

export function AdminAttributeForm({
  attributeForm,
  isMutatingAttribute,
  isSelectedAttributeAvailable,
  isSelectedCategoryAvailable,
  onDeleteAttribute,
  onFormPatch,
  onSubmitAttribute,
  onTypeChange,
}: AdminAttributeFormProps) {
  return (
    <form className="admin-attribute-form" onSubmit={onSubmitAttribute}>
      <label className="form-field">
        <span>Название</span>
        <input
          disabled={!isSelectedCategoryAvailable}
          onChange={(event) => onFormPatch({ name: event.target.value })}
          required
          value={attributeForm.name}
        />
      </label>
      <label className="form-field">
        <span>Код</span>
        <input
          disabled={!isSelectedCategoryAvailable}
          onChange={(event) => onFormPatch({ code: event.target.value })}
          required
          value={attributeForm.code}
        />
      </label>
      <label className="form-field">
        <span>Тип</span>
        <select disabled={!isSelectedCategoryAvailable} onChange={(event) => onTypeChange(event.target.value)} value={attributeForm.type}>
          {attributeTypes.map((type) => (
            <option key={type.value} value={type.value}>
              {type.label}
            </option>
          ))}
        </select>
      </label>
      <label className="form-field">
        <span>Единица</span>
        <input
          disabled={!isSelectedCategoryAvailable}
          onChange={(event) => onFormPatch({ unit: event.target.value })}
          value={attributeForm.unit}
        />
      </label>
      <label className="form-field">
        <span>Сортировка</span>
        <input
          disabled={!isSelectedCategoryAvailable}
          inputMode="numeric"
          onChange={(event) => onFormPatch({ sortOrder: event.target.value })}
          type="number"
          value={attributeForm.sortOrder}
        />
      </label>

      <div className="admin-attribute-manager__checks">
        <AttributeCheckbox
          checked={attributeForm.isRequired}
          disabled={!isSelectedCategoryAvailable}
          label="Обязательная"
          onChange={(isRequired) => onFormPatch({ isRequired })}
        />
        <AttributeCheckbox
          checked={attributeForm.isFilterable}
          disabled={!isSelectedCategoryAvailable}
          label="Фильтруемая"
          onChange={(isFilterable) => onFormPatch({ isFilterable })}
        />
        <AttributeCheckbox
          checked={attributeForm.isComparable}
          disabled={!isSelectedCategoryAvailable}
          label="Сравниваемая"
          onChange={(isComparable) => onFormPatch({ isComparable })}
        />
        <AttributeCheckbox
          checked={attributeForm.isVisibleInProduct}
          disabled={!isSelectedCategoryAvailable}
          label="В карточке товара"
          onChange={(isVisibleInProduct) => onFormPatch({ isVisibleInProduct })}
        />
        <AttributeCheckbox
          checked={attributeForm.isSeoImportant}
          disabled={!isSelectedCategoryAvailable}
          label="SEO-важная"
          onChange={(isSeoImportant) => onFormPatch({ isSeoImportant })}
        />
        <AttributeCheckbox
          checked={attributeForm.isUsedInGeneratedName}
          disabled={!isSelectedCategoryAvailable}
          label="В названии товара"
          onChange={(isUsedInGeneratedName) => onFormPatch({ isUsedInGeneratedName })}
        />
        <AttributeCheckbox
          checked={attributeForm.isActive}
          disabled={!isSelectedCategoryAvailable}
          label="Активна"
          onChange={(isActive) => onFormPatch({ isActive })}
        />
      </div>

      <div className="admin-attribute-manager__actions">
        <button className="button button--primary" disabled={!isSelectedCategoryAvailable || isMutatingAttribute} type="submit">
          {isSelectedAttributeAvailable ? "Сохранить характеристику" : "Создать характеристику"}
        </button>
        <button
          className="button button--ghost"
          disabled={!isSelectedAttributeAvailable || isMutatingAttribute}
          onClick={onDeleteAttribute}
          type="button"
        >
          Удалить характеристику
        </button>
      </div>
    </form>
  );
}

function AttributeCheckbox({
  checked,
  disabled,
  label,
  onChange,
}: {
  checked: boolean;
  disabled: boolean;
  label: string;
  onChange: (checked: boolean) => void;
}) {
  return (
    <label className="admin-attribute-manager__check">
      <input checked={checked} disabled={disabled} onChange={(event) => onChange(event.target.checked)} type="checkbox" />
      <span>{label}</span>
    </label>
  );
}
