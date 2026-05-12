"use client";

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
  type AdminBrandLogo,
  type UpsertAdminBrandCommand,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";

type AdminBrandManagerProps = {
  csrfToken?: string | null;
};

type BrandFormState = {
  name: string;
  slug: string;
  description: string;
  seoTitle: string;
  seoDescription: string;
  isActive: boolean;
};

type LogoPreviewState = {
  url: string;
  originalFileName: string;
};

const emptyForm: BrandFormState = {
  name: "",
  slug: "",
  description: "",
  seoTitle: "",
  seoDescription: "",
  isActive: true,
};

const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

export function AdminBrandManager({ csrfToken = null }: AdminBrandManagerProps) {
  const [brands, setBrands] = useState<AdminBrandListItem[]>([]);
  const [selectedBrand, setSelectedBrand] = useState<AdminBrandDetail | null>(null);
  const [form, setForm] = useState<BrandFormState>(emptyForm);
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

  const listParams = useMemo<AdminBrandListParams>(() => {
    const params: AdminBrandListParams = {};
    const normalizedSearch = search.trim();

    if (normalizedSearch) {
      params.search = normalizedSearch;
    }

    if (activeFilter === "true") {
      params.isActive = true;
    } else if (activeFilter === "false") {
      params.isActive = false;
    }

    return params;
  }, [activeFilter, search]);

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
      setForm(formFromDetail(detail));
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
    setForm(emptyForm);
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
      const command = buildCommand(form);
      const savedBrand = selectedBrand
        ? await updateAdminBrand(selectedBrand.id, command, csrfToken)
        : await createAdminBrand(command, csrfToken);

      if (!isCurrentBrandMutation(capturedBrandId, capturedEditorSession)) return;

      setSelectedBrand(savedBrand);
      setForm(formFromDetail(savedBrand));
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
      setForm(emptyForm);
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

  return (
    <div className="admin-brand-manager">
      <section className="admin-catalog-table admin-brand-manager__list" aria-labelledby="admin-brand-list-title">
        <div className="admin-brand-manager__head">
          <div>
            <h2 id="admin-brand-list-title">Бренды</h2>
            <p>Фильтры, статус и быстрый выбор бренда.</p>
          </div>
          <button className="button button--primary" onClick={startCreate} type="button">
            Новый бренд
          </button>
        </div>

        <div className="admin-brand-manager__filters">
          <label className="admin-filter-field">
            <span>Поиск</span>
            <input
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Название или слаг"
              type="search"
              value={search}
            />
          </label>
          <label className="admin-filter-field">
            <span>Активность</span>
            <select onChange={(event) => setActiveFilter(event.target.value)} value={activeFilter}>
              <option value="">Все</option>
              <option value="true">Активные</option>
              <option value="false">Неактивные</option>
            </select>
          </label>
        </div>

        <div className="admin-brand-manager__rows" aria-busy={isLoadingList}>
          {brands.length ? (
            brands.map((brand) => (
              <button
                aria-pressed={selectedId === brand.id}
                className="admin-brand-row"
                key={brand.id}
                onClick={() => selectBrand(brand.id)}
                type="button"
              >
                <span>
                  <strong>{brand.name}</strong>
                  <small>{brand.slug}</small>
                </span>
                <span className="admin-brand-row__meta">
                  {brand.isActive ? "Активен" : "Неактивен"} · {brand.productsCount} товаров
                </span>
              </button>
            ))
          ) : (
            <p className="empty-state">Бренды не найдены.</p>
          )}
        </div>
      </section>

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
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              required
              value={form.name}
            />
          </label>
          <label className="form-field">
            <span>Слаг</span>
            <input
              onChange={(event) => setForm((current) => ({ ...current, slug: event.target.value }))}
              required
              value={form.slug}
            />
          </label>
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
              <img alt={logoAlt} src={logoPreview.url} />
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

function formFromDetail(brand: AdminBrandDetail): BrandFormState {
  return {
    name: brand.name,
    slug: brand.slug,
    description: brand.description ?? "",
    seoTitle: brand.seoTitle ?? "",
    seoDescription: brand.seoDescription ?? "",
    isActive: brand.isActive,
  };
}

function buildCommand(form: BrandFormState): UpsertAdminBrandCommand {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    description: normalizeOptionalText(form.description),
    seoTitle: normalizeOptionalText(form.seoTitle),
    seoDescription: normalizeOptionalText(form.seoDescription),
    isActive: form.isActive,
  };
}

function logoPreviewFromUpload(logo: AdminBrandLogo): LogoPreviewState {
  return {
    url: logo.url,
    originalFileName: logo.originalFileName,
  };
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}
