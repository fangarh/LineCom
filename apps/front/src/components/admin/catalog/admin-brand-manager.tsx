"use client";

import Image from "next/image";
import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import {
  createAdminBrand,
  deleteAdminBrand,
  deleteAdminBrandLogo,
  getAdminBrand,
  getAdminBrands,
  updateAdminBrand,
  uploadAdminBrandLogo,
  type AdminBrandDetail,
  type AdminBrandListItem,
  type AdminBrandListParams,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import { generateSlug } from "@/lib/catalog/slug";
import { AdminBrandListPanel } from "./admin-brand-list-panel";
import {
  brandFormFromDetail,
  buildBrandCommand,
  buildBrandListParams,
  emptyBrandForm,
  logoPreviewFromUpload,
  type BrandFormState,
  type LogoPreviewState,
} from "./admin-brand-manager-helpers";

type AdminBrandManagerProps = {
  csrfToken?: string | null;
};

const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

export function AdminBrandManager({ csrfToken = null }: AdminBrandManagerProps) {
  const [brands, setBrands] = useState<AdminBrandListItem[]>([]);
  const [selectedBrand, setSelectedBrand] = useState<AdminBrandDetail | null>(null);
  const [form, setForm] = useState<BrandFormState>(emptyBrandForm);
  const [isSlugManual, setIsSlugManual] = useState(false);
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<LogoPreviewState | null>(null);
  const [isLoadingList, setIsLoadingList] = useState(false);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isMutating, setIsMutating] = useState(false);
  const [isLogoMutating, setIsLogoMutating] = useState(false);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [logoStatusMessage, setLogoStatusMessage] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const listRequestSeqRef = useRef(0);
  const detailRequestSeqRef = useRef(0);
  const brandEditorSessionRef = useRef(0);
  const selectedBrandIdRef = useRef<string | null>(null);
  const latestListParamsRef = useRef<AdminBrandListParams>({});

  const listParams = useMemo<AdminBrandListParams>(() => buildBrandListParams(search, activeFilter), [activeFilter, search]);

  useEffect(() => {
    selectedBrandIdRef.current = selectedBrand?.id ?? null;
  }, [selectedBrand?.id]);

  useEffect(() => {
    latestListParamsRef.current = listParams;
  }, [listParams]);

  const loadBrandsForParams = useCallback(async (params: AdminBrandListParams) => {
    const requestSeq = listRequestSeqRef.current + 1;
    listRequestSeqRef.current = requestSeq;
    setIsLoadingList(true);
    setAlertMessage(null);

    try {
      const response = await getAdminBrands(params);
      if (listRequestSeqRef.current !== requestSeq) return;

      setBrands(response.items);
    } catch (error) {
      if (listRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (listRequestSeqRef.current === requestSeq) {
        setIsLoadingList(false);
      }
    }
  }, []);

  useEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        void loadBrandsForParams(listParams);
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [listParams, loadBrandsForParams]);

  const refreshBrandList = useCallback(async () => {
    await loadBrandsForParams(latestListParamsRef.current);
  }, [loadBrandsForParams]);

  async function selectBrand(brandId: string) {
    const requestSeq = detailRequestSeqRef.current + 1;
    detailRequestSeqRef.current = requestSeq;
    brandEditorSessionRef.current += 1;
    setIsLoadingDetail(true);
    setAlertMessage(null);
    setStatusMessage(null);
    setLogoStatusMessage(null);

    try {
      const detail = await getAdminBrand(brandId);
      if (detailRequestSeqRef.current !== requestSeq) return;

      setSelectedBrand(detail);
      setForm(brandFormFromDetail(detail));
      setIsSlugManual(true);
      setLogoFile(null);
      setLogoPreview(null);
      clearLogoFileInput();
    } catch (error) {
      if (detailRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (detailRequestSeqRef.current === requestSeq) {
        setIsLoadingDetail(false);
      }
    }
  }

  function startCreate() {
    detailRequestSeqRef.current += 1;
    brandEditorSessionRef.current += 1;
    setSelectedBrand(null);
    setForm(emptyBrandForm);
    setIsSlugManual(false);
    setLogoFile(null);
    setLogoPreview(null);
    clearLogoFileInput();
    setAlertMessage(null);
    setStatusMessage(null);
    setLogoStatusMessage(null);
  }

  async function submitBrand(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const capturedBrandId = selectedBrand?.id ?? null;
    const capturedEditorSession = brandEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildBrandCommand(form);
      const savedBrand = selectedBrand
        ? await updateAdminBrand(selectedBrand.id, command, csrfToken)
        : await createAdminBrand(command, csrfToken);

      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;

      setSelectedBrand(savedBrand);
      setForm(brandFormFromDetail(savedBrand));
      setIsSlugManual(true);
      setStatusMessage(selectedBrand ? "Бренд сохранен." : "Бренд создан.");
      await refreshBrandList();
    } catch (error) {
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function deleteSelectedBrand() {
    if (!selectedBrand) return;
    const capturedBrandId = selectedBrand.id;
    const capturedEditorSession = brandEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await deleteAdminBrand(selectedBrand.id, csrfToken);
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;

      setSelectedBrand(null);
      setForm(emptyBrandForm);
      setIsSlugManual(false);
      setLogoFile(null);
      setLogoPreview(null);
      clearLogoFileInput();
      setStatusMessage("Бренд удален.");
      await refreshBrandList();
    } catch (error) {
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  function selectLogoFile(event: ChangeEvent<HTMLInputElement>) {
    setLogoFile(event.target.files?.[0] ?? null);
    setLogoStatusMessage(null);
  }

  async function uploadSelectedLogo() {
    if (!selectedBrand || !logoFile) return;
    const capturedBrandId = selectedBrand.id;
    const capturedEditorSession = brandEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setIsLogoMutating(true);
    setAlertMessage(null);
    setLogoStatusMessage(null);

    try {
      const logo = await uploadAdminBrandLogo(selectedBrand.id, logoFile, csrfToken);
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;

      setLogoPreview(logoPreviewFromUpload(logo));
      setLogoStatusMessage("Логотип загружен.");
      setLogoFile(null);
      clearLogoFileInput();
    } catch (error) {
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsLogoMutating(false);
    }
  }

  async function deleteSelectedLogo() {
    if (!selectedBrand) return;
    const capturedBrandId = selectedBrand.id;
    const capturedEditorSession = brandEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setIsLogoMutating(true);
    setAlertMessage(null);
    setLogoStatusMessage(null);

    try {
      await deleteAdminBrandLogo(selectedBrand.id, csrfToken);
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;

      setLogoPreview(null);
      setLogoFile(null);
      clearLogoFileInput();
      setSelectedBrand((current) => (current ? { ...current, logoFileId: null } : current));
      setLogoStatusMessage("Логотип удален.");
    } catch (error) {
      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsLogoMutating(false);
    }
  }

  const selectedId = selectedBrand?.id ?? null;
  const logoAlt = `Логотип ${form.name.trim() || selectedBrand?.name || "бренда"}`;
  const canDeleteLogo = Boolean(selectedBrand && (selectedBrand.logoFileId || logoPreview));

  function isCurrentBrandMutation(capturedBrandId: string | null, capturedEditorSession: number) {
    return brandEditorSessionRef.current === capturedEditorSession && selectedBrandIdRef.current === capturedBrandId;
  }

  function clearLogoFileInput() {
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }

  function changeBrandName(name: string) {
    setForm((current) => ({
      ...current,
      name,
      slug: isSlugManual ? current.slug : generateSlug(name),
    }));
  }

  function changeBrandSlug(slug: string) {
    setIsSlugManual(true);
    setForm((current) => ({ ...current, slug }));
  }

  function regenerateBrandSlug() {
    setIsSlugManual(true);
    setForm((current) => ({ ...current, slug: generateSlug(current.name) }));
  }

  return (
    <div className="admin-brand-manager">
      <AdminBrandListPanel
        activeFilter={activeFilter}
        brands={brands}
        isLoadingList={isLoadingList}
        onActiveFilterChange={setActiveFilter}
        onCreateBrand={startCreate}
        onSearchChange={setSearch}
        onSelectBrand={selectBrand}
        search={search}
        selectedBrandId={selectedId}
      />

      <section className="admin-catalog-form admin-brand-manager__editor" aria-label="Редактор бренда">
        <div className="admin-brand-manager__head">
          <div>
            <h2>{selectedBrand ? "Редактирование бренда" : "Новый бренд"}</h2>
            <p className="admin-catalog-status">
              {isLoadingDetail ? "Загружаем карточку..." : selectedBrand ? selectedBrand.slug : "Заполните поля."}
            </p>
          </div>
        </div>

        {alertMessage ? (
          <p className="form-alert" role="alert">
            {alertMessage}
          </p>
        ) : null}
        {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

        <form className="admin-brand-form" onSubmit={submitBrand}>
          <label className="form-field">
            <span>Название</span>
            <input
              onChange={(event) => changeBrandName(event.target.value)}
              required
              value={form.name}
            />
          </label>
          <label className="form-field">
            <span>Слаг</span>
            <input
              onChange={(event) => changeBrandSlug(event.target.value)}
              onFocus={(event) => event.currentTarget.select()}
              required
              value={form.slug}
            />
          </label>
          <button className="button button--ghost" onClick={regenerateBrandSlug} type="button">
            Сгенерировать заново
          </button>
          <label className="form-field">
            <span>Описание</span>
            <textarea
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              rows={4}
              value={form.description}
            />
          </label>
          <label className="form-field">
            <span>SEO-заголовок</span>
            <input
              onChange={(event) => setForm((current) => ({ ...current, seoTitle: event.target.value }))}
              value={form.seoTitle}
            />
          </label>
          <label className="form-field">
            <span>SEO-описание</span>
            <textarea
              onChange={(event) => setForm((current) => ({ ...current, seoDescription: event.target.value }))}
              rows={3}
              value={form.seoDescription}
            />
          </label>
          <label className="admin-brand-manager__check">
            <input
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              type="checkbox"
            />
            <span>Активен</span>
          </label>

          <div className="admin-brand-manager__actions">
            <button className="button button--primary" disabled={isMutating} type="submit">
              {selectedBrand ? "Сохранить" : "Создать"}
            </button>
            <button className="button button--ghost" disabled={!selectedBrand || isMutating} onClick={deleteSelectedBrand} type="button">
              Удалить
            </button>
          </div>
        </form>

        <section className="admin-brand-manager__logo" aria-label="Логотип бренда">
          <div className="admin-brand-manager__logo-preview">
            {logoPreview ? (
              <Image alt={logoAlt} height={96} src={logoPreview.url} unoptimized width={240} />
            ) : (
              <p className="admin-catalog-status">
                {selectedBrand?.logoFileId
                  ? "Логотип загружен. Предпросмотр появится после замены файла."
                  : "Логотип пока не загружен."}
              </p>
            )}
          </div>
          <label className="form-field">
            <span>Файл логотипа</span>
            <input
              accept="image/*"
              disabled={!selectedBrand || isLogoMutating}
              onChange={selectLogoFile}
              ref={fileInputRef}
              type="file"
            />
          </label>
          {logoFile ? <p className="admin-catalog-status">Выбран файл: {logoFile.name}</p> : null}
          {logoStatusMessage ? <p className="form-success">{logoStatusMessage}</p> : null}
          <div className="admin-brand-manager__actions">
            <button
              className="button button--secondary"
              disabled={!selectedBrand || !logoFile || isLogoMutating}
              onClick={uploadSelectedLogo}
              type="button"
            >
              Заменить логотип
            </button>
            <button
              className="button button--ghost"
              disabled={!canDeleteLogo || isLogoMutating}
              onClick={deleteSelectedLogo}
              type="button"
            >
              Удалить логотип
            </button>
          </div>
        </section>
      </section>
    </div>
  );
}
