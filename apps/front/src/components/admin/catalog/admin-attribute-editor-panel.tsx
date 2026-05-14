import type { FormEvent } from "react";
import type { AdminAttributeOption, AdminCategoryAttribute, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { AdminAttributeForm } from "./admin-attribute-form";
import { AdminAttributeOptionEditor } from "./admin-attribute-option-editor";
import type { AttributeFormState, OptionFormState } from "./admin-attribute-manager-helpers";

type AdminAttributeEditorPanelProps = {
  alertMessage: string | null;
  attributeForm: AttributeFormState;
  isMutatingAttribute: boolean;
  isMutatingOption: boolean;
  isPersistedSelectAttribute: boolean;
  onAttributeFormPatch: (patch: Partial<AttributeFormState>) => void;
  onAttributeTypeChange: (type: string) => void;
  onDeleteAttribute: () => void;
  onDeleteOption: () => void;
  onOptionFormPatch: (patch: Partial<OptionFormState>) => void;
  onOptionSlugChange: (slug: string) => void;
  onOptionValueChange: (value: string) => void;
  onRegenerateOptionSlug: () => void;
  onSelectOption: (option: AdminAttributeOption) => void;
  onStartCreateOption: () => void;
  onSubmitAttribute: (event: FormEvent<HTMLFormElement>) => void;
  onSubmitOption: (event: FormEvent<HTMLFormElement>) => void;
  optionForm: OptionFormState;
  selectedAttribute: AdminCategoryAttribute | null;
  selectedCategory: AdminCategoryListItem | null;
  selectedCategoryId: string;
  selectedOption: AdminAttributeOption | null;
  statusMessage: string | null;
};

export function AdminAttributeEditorPanel({
  alertMessage,
  attributeForm,
  isMutatingAttribute,
  isMutatingOption,
  isPersistedSelectAttribute,
  onAttributeFormPatch,
  onAttributeTypeChange,
  onDeleteAttribute,
  onDeleteOption,
  onOptionFormPatch,
  onOptionSlugChange,
  onOptionValueChange,
  onRegenerateOptionSlug,
  onSelectOption,
  onStartCreateOption,
  onSubmitAttribute,
  onSubmitOption,
  optionForm,
  selectedAttribute,
  selectedCategory,
  selectedCategoryId,
  selectedOption,
  statusMessage,
}: AdminAttributeEditorPanelProps) {
  return (
    <section className="admin-catalog-form admin-attribute-manager__editor" aria-label="Редактор характеристики">
      <div className="admin-attribute-manager__head">
        <div>
          <h2>{selectedAttribute ? "Редактирование характеристики" : "Новая характеристика"}</h2>
          <p className="admin-catalog-status">
            {selectedCategory ? selectedCategory.name : "Категория не выбрана."}
          </p>
        </div>
      </div>

      {alertMessage ? (
        <p className="form-alert" role="alert">
          {alertMessage}
        </p>
      ) : null}
      {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

      <AdminAttributeForm
        attributeForm={attributeForm}
        isMutatingAttribute={isMutatingAttribute}
        isSelectedAttributeAvailable={Boolean(selectedAttribute)}
        isSelectedCategoryAvailable={Boolean(selectedCategoryId)}
        onDeleteAttribute={onDeleteAttribute}
        onFormPatch={onAttributeFormPatch}
        onSubmitAttribute={onSubmitAttribute}
        onTypeChange={onAttributeTypeChange}
      />

      {isPersistedSelectAttribute && selectedAttribute ? (
        <AdminAttributeOptionEditor
          isMutatingOption={isMutatingOption}
          onDeleteOption={onDeleteOption}
          onFormPatch={onOptionFormPatch}
          onOptionSlugChange={onOptionSlugChange}
          onOptionValueChange={onOptionValueChange}
          onRegenerateOptionSlug={onRegenerateOptionSlug}
          onSelectOption={onSelectOption}
          onStartCreateOption={onStartCreateOption}
          onSubmitOption={onSubmitOption}
          optionForm={optionForm}
          selectedAttribute={selectedAttribute}
          selectedOption={selectedOption}
        />
      ) : null}
    </section>
  );
}
