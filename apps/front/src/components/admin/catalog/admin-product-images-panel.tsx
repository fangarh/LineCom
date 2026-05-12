"use client";

import { useCallback, useEffect, useRef, useState, type ChangeEvent } from "react";
import {
  deleteAdminProductImage,
  getAdminProductImages,
  setAdminProductMainImage,
  updateAdminProductImage,
  updateAdminProductImageOrder,
  uploadAdminProductImages,
  type AdminProductImage,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";

const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminProductImagesPanelProps = {
  productId: string | null;
  csrfToken?: string | null;
};

type ImageFormState = {
  alt: string;
  title: string;
};

type MutatingImageState = {
  productId: string;
  imageId: string;
};

export function AdminProductImagesPanel({ productId, csrfToken = null }: AdminProductImagesPanelProps) {
  const [images, setImages] = useState<AdminProductImage[]>([]);
  const [imagesProductId, setImagesProductId] = useState<string | null>(null);
  const [forms, setForms] = useState<Record<string, ImageFormState>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [mutatingImage, setMutatingImage] = useState<MutatingImageState | null>(null);
  const [uploadingProductId, setUploadingProductId] = useState<string | null>(null);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const requestSeqRef = useRef(0);
  const operationSeqRef = useRef(0);
  const isMountedRef = useRef(false);
  const productIdRef = useRef<string | null>(null);

  const isCurrentOperation = useCallback((targetProductId: string, operationSeq: number) => {
    return isMountedRef.current && operationSeqRef.current === operationSeq && productIdRef.current === targetProductId;
  }, []);

  const loadImages = useCallback(async (targetProductId: string, operationSeq: number) => {
    const requestSeq = requestSeqRef.current + 1;
    requestSeqRef.current = requestSeq;
    if (isCurrentOperation(targetProductId, operationSeq)) {
      setIsLoading(true);
      setAlertMessage(null);
    }

    try {
      const response = await getAdminProductImages(targetProductId);
      if (requestSeqRef.current !== requestSeq || !isCurrentOperation(targetProductId, operationSeq)) return;
      setImages(response.items);
      setImagesProductId(targetProductId);
      setForms(formsFromImages(response.items));
    } catch (error) {
      if (requestSeqRef.current !== requestSeq || !isCurrentOperation(targetProductId, operationSeq)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (requestSeqRef.current === requestSeq && isCurrentOperation(targetProductId, operationSeq)) {
        setIsLoading(false);
      }
    }
  }, [isCurrentOperation]);

  useEffect(() => {
    isMountedRef.current = true;

    return () => {
      isMountedRef.current = false;
      operationSeqRef.current += 1;
    };
  }, []);

  useEffect(() => {
    requestSeqRef.current += 1;
    operationSeqRef.current += 1;
    productIdRef.current = productId;
    const operationSeq = operationSeqRef.current;
    let isCancelled = false;

    queueMicrotask(() => {
      if (isCancelled || !isMountedRef.current || operationSeqRef.current !== operationSeq) return;
      setUploadingProductId(null);
      setMutatingImage(null);
      if (productId) {
        void loadImages(productId, operationSeq);
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [loadImages, productId]);

  async function uploadFiles(event: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []);
    event.target.value = "";
    if (!productId || files.length === 0) return;
    const operationProductId = productId;
    const operationSeq = operationSeqRef.current;
    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setUploadingProductId(operationProductId);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await uploadAdminProductImages(operationProductId, files, csrfToken);
      if (!isCurrentOperation(operationProductId, operationSeq)) return;
      setStatusMessage("Изображения загружены.");
      await loadImages(operationProductId, operationSeq);
    } catch (error) {
      if (!isCurrentOperation(operationProductId, operationSeq)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentOperation(operationProductId, operationSeq)) {
        setUploadingProductId(null);
      }
    }
  }

  async function saveMetadata(imageId: string) {
    if (!productId) return;
    const operationProductId = productId;
    const operationSeq = operationSeqRef.current;
    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const form = forms[imageId] ?? { alt: "", title: "" };
    setMutatingImage({ productId: operationProductId, imageId });
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await updateAdminProductImage(
        operationProductId,
        imageId,
        { alt: normalizeOptionalText(form.alt), title: normalizeOptionalText(form.title) },
        csrfToken,
      );
      if (!isCurrentOperation(operationProductId, operationSeq)) return;
      setStatusMessage("Метаданные изображения сохранены.");
      await loadImages(operationProductId, operationSeq);
    } catch (error) {
      if (!isCurrentOperation(operationProductId, operationSeq)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentOperation(operationProductId, operationSeq)) {
        setMutatingImage(null);
      }
    }
  }

  async function makeMain(imageId: string) {
    if (!productId) return;
    const operationProductId = productId;
    const operationSeq = operationSeqRef.current;
    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    await mutateImage(operationProductId, operationSeq, imageId, "Основное изображение обновлено.", async () => {
      await setAdminProductMainImage(operationProductId, imageId, csrfToken);
    });
  }

  async function reorderImage(imageId: string, direction: -1 | 1) {
    if (!productId) return;
    const operationProductId = productId;
    const operationSeq = operationSeqRef.current;
    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const index = images.findIndex((image) => image.id === imageId);
    const targetIndex = index + direction;
    if (index < 0 || targetIndex < 0 || targetIndex >= images.length) return;

    const orderedImages = [...images];
    [orderedImages[index], orderedImages[targetIndex]] = [orderedImages[targetIndex], orderedImages[index]];

    await mutateImage(operationProductId, operationSeq, imageId, "Порядок изображений обновлен.", async () => {
      await updateAdminProductImageOrder(
        operationProductId,
        orderedImages.map((image) => image.id),
        csrfToken,
      );
    });
  }

  async function deleteImage(imageId: string) {
    if (!productId) return;
    const operationProductId = productId;
    const operationSeq = operationSeqRef.current;
    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    await mutateImage(operationProductId, operationSeq, imageId, "Изображение удалено.", async () => {
      await deleteAdminProductImage(operationProductId, imageId, csrfToken);
    });
  }

  async function mutateImage(operationProductId: string, operationSeq: number, imageId: string, successMessage: string, action: () => Promise<void>) {
    setMutatingImage({ productId: operationProductId, imageId });
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await action();
      if (!isCurrentOperation(operationProductId, operationSeq)) return;
      setStatusMessage(successMessage);
      await loadImages(operationProductId, operationSeq);
    } catch (error) {
      if (!isCurrentOperation(operationProductId, operationSeq)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentOperation(operationProductId, operationSeq)) {
        setMutatingImage(null);
      }
    }
  }

  if (!productId) {
    return <p className="admin-catalog-status">Выберите товар.</p>;
  }

  const visibleImages = imagesProductId === productId ? images : [];
  const visibleForms = imagesProductId === productId ? forms : {};
  const visibleStatusMessage = imagesProductId === productId ? statusMessage : null;
  const isUploadingCurrentProduct = uploadingProductId === productId;

  return (
    <div className="admin-product-images">
      {alertMessage ? (
        <p className="form-alert" role="alert">
          {alertMessage}
        </p>
      ) : null}
      {visibleStatusMessage ? <p className="form-success">{visibleStatusMessage}</p> : null}

      <label className="form-field admin-product-images__upload">
        <span>Загрузить изображения</span>
        <input
          accept="image/*"
          disabled={isUploadingCurrentProduct}
          multiple
          onChange={uploadFiles}
          type="file"
        />
      </label>

      <div className="admin-product-images__grid" aria-busy={isLoading}>
        {visibleImages.length ? (
          visibleImages.map((image, index) => {
            const form = visibleForms[image.id] ?? { alt: image.alt, title: image.title ?? "" };
            const isMutating = mutatingImage?.productId === productId && mutatingImage.imageId === image.id;

            return (
              <article className="admin-product-image-card" key={image.id} aria-label={image.originalFileName}>
                <div className="admin-product-image-card__preview">
                  <img alt={image.alt || image.originalFileName} src={image.url} />
                </div>
                <div className="admin-product-image-card__body">
                  <div className="admin-product-image-card__head">
                    <strong>{image.originalFileName}</strong>
                    {image.isMain ? <span className="status-pill">Основное</span> : null}
                  </div>
                  <div className="admin-product-image-card__form">
                    <label className="form-field">
                      <span>Alt</span>
                      <input
                        onChange={(event) =>
                          setForms((current) => ({
                            ...current,
                            [image.id]: { ...form, alt: event.target.value },
                          }))
                        }
                        value={form.alt}
                      />
                    </label>
                    <label className="form-field">
                      <span>Title</span>
                      <input
                        onChange={(event) =>
                          setForms((current) => ({
                            ...current,
                            [image.id]: { ...form, title: event.target.value },
                          }))
                        }
                        value={form.title}
                      />
                    </label>
                    <div className="admin-product-image-card__actions">
                      <button
                        className="button button--secondary"
                        disabled={isMutating}
                        onClick={() => saveMetadata(image.id)}
                        type="button"
                      >
                        Сохранить метаданные
                      </button>
                      <button
                        className="button button--ghost"
                        disabled={image.isMain || isMutating}
                        onClick={() => makeMain(image.id)}
                        type="button"
                      >
                        Сделать основным
                      </button>
                      <button
                        className="button button--ghost"
                        disabled={index === 0 || isMutating}
                        onClick={() => reorderImage(image.id, -1)}
                        type="button"
                      >
                        Выше
                      </button>
                      <button
                        className="button button--ghost"
                        disabled={index === visibleImages.length - 1 || isMutating}
                        onClick={() => reorderImage(image.id, 1)}
                        type="button"
                      >
                        Ниже
                      </button>
                      <button
                        className="button button--ghost"
                        disabled={isMutating}
                        onClick={() => deleteImage(image.id)}
                        type="button"
                      >
                        Удалить
                      </button>
                    </div>
                  </div>
                </div>
              </article>
            );
          })
        ) : (
          <p className="empty-state">Изображения не загружены.</p>
        )}
      </div>
    </div>
  );
}

function formsFromImages(images: AdminProductImage[]) {
  return images.reduce<Record<string, ImageFormState>>((forms, image) => {
    forms[image.id] = {
      alt: image.alt,
      title: image.title ?? "",
    };
    return forms;
  }, {});
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}
