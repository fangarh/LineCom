import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminProductImagesPanel } from "./admin-product-images-panel";
import type { AdminProductImage, AdminProductImagesResponse } from "@/lib/api/admin-catalog";

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminProductImages: vi.fn(),
  uploadAdminProductImages: vi.fn(),
  updateAdminProductImage: vi.fn(),
  updateAdminProductImageOrder: vi.fn(),
  setAdminProductMainImage: vi.fn(),
  deleteAdminProductImage: vi.fn(),
}));

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminProductImages: adminCatalogApiMock.getAdminProductImages,
    uploadAdminProductImages: adminCatalogApiMock.uploadAdminProductImages,
    updateAdminProductImage: adminCatalogApiMock.updateAdminProductImage,
    updateAdminProductImageOrder: adminCatalogApiMock.updateAdminProductImageOrder,
    setAdminProductMainImage: adminCatalogApiMock.setAdminProductMainImage,
    deleteAdminProductImage: adminCatalogApiMock.deleteAdminProductImage,
  };
});

const mainImage: AdminProductImage = {
  id: "image-main",
  storedFileId: "file-main",
  url: "/uploads/main.jpg",
  originalFileName: "main.jpg",
  contentType: "image/jpeg",
  sizeBytes: 1024,
  checksum: "checksum-main",
  alt: "Кабель на белом фоне",
  title: "Кабель ВВГнг",
  sortOrder: 10,
  isMain: true,
  createdAt: "2026-05-12T08:00:00Z",
};

const detailImage: AdminProductImage = {
  id: "image-detail",
  storedFileId: "file-detail",
  url: "/uploads/detail.jpg",
  originalFileName: "detail.jpg",
  contentType: "image/jpeg",
  sizeBytes: 2048,
  checksum: "checksum-detail",
  alt: "Маркировка кабеля",
  title: null,
  sortOrder: 20,
  isMain: false,
  createdAt: "2026-05-12T08:05:00Z",
};

const nextProductImage: AdminProductImage = {
  id: "image-next",
  storedFileId: "file-next",
  url: "/uploads/next.jpg",
  originalFileName: "next.jpg",
  contentType: "image/jpeg",
  sizeBytes: 3072,
  checksum: "checksum-next",
  alt: "Фото другого товара",
  title: "Другой товар",
  sortOrder: 10,
  isMain: true,
  createdAt: "2026-05-12T08:10:00Z",
};

function imagesResponse(items: AdminProductImage[] = [mainImage, detailImage]): AdminProductImagesResponse {
  return { items };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((promiseResolve, promiseReject) => {
    resolve = promiseResolve;
    reject = promiseReject;
  });

  return { promise, resolve, reject };
}

async function renderPanel(csrfToken = "csrf-token") {
  render(<AdminProductImagesPanel productId="product-active" csrfToken={csrfToken} />);

  await screen.findByRole("article", { name: /main\.jpg/ });
}

describe("AdminProductImagesPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    adminCatalogApiMock.getAdminProductImages.mockResolvedValue(imagesResponse());
    adminCatalogApiMock.uploadAdminProductImages.mockResolvedValue(imagesResponse());
    adminCatalogApiMock.updateAdminProductImage.mockResolvedValue(mainImage);
    adminCatalogApiMock.updateAdminProductImageOrder.mockResolvedValue(imagesResponse([detailImage, mainImage]));
    adminCatalogApiMock.setAdminProductMainImage.mockResolvedValue(detailImage);
    adminCatalogApiMock.deleteAdminProductImage.mockResolvedValue(undefined);
  });

  it("загружает изображения товара и показывает миниатюры с именами файлов", async () => {
    await renderPanel();

    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledWith("product-active");
    const preview = screen.getByRole("img", { name: "Кабель на белом фоне" });
    expect(preview).toHaveAttribute("src", "/uploads/main.jpg");
    expect(preview).toHaveAttribute("width", "150");
    expect(preview).toHaveAttribute("height", "118");
    expect(screen.getByText("main.jpg")).toBeInTheDocument();
    expect(screen.getByText("Основное")).toBeInTheDocument();
  });

  it("передает все выбранные файлы в uploadAdminProductImages с CSRF-токеном", async () => {
    const user = userEvent.setup();
    await renderPanel();
    const firstFile = new File(["one"], "one.jpg", { type: "image/jpeg" });
    const secondFile = new File(["two"], "two.png", { type: "image/png" });

    await user.upload(screen.getByLabelText("Загрузить изображения"), [firstFile, secondFile]);

    expect(adminCatalogApiMock.uploadAdminProductImages).toHaveBeenCalledWith(
      "product-active",
      [firstFile, secondFile],
      "csrf-token",
    );
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(2);
  });

  it("сохраняет alt и title через endpoint метаданных изображения", async () => {
    const user = userEvent.setup();
    await renderPanel();
    const card = screen.getByRole("article", { name: /main\.jpg/ });

    await user.clear(within(card).getByLabelText("Alt"));
    await user.type(within(card).getByLabelText("Alt"), "Новый alt");
    await user.clear(within(card).getByLabelText("Title"));
    await user.type(within(card).getByLabelText("Title"), "Новый title");
    await user.click(within(card).getByRole("button", { name: "Сохранить метаданные" }));

    expect(adminCatalogApiMock.updateAdminProductImage).toHaveBeenCalledWith(
      "product-active",
      "image-main",
      { alt: "Новый alt", title: "Новый title" },
      "csrf-token",
    );
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(2);
  });

  it("назначает основное изображение через main endpoint", async () => {
    const user = userEvent.setup();
    await renderPanel();
    const card = screen.getByRole("article", { name: /detail\.jpg/ });

    await user.click(within(card).getByRole("button", { name: "Сделать основным" }));

    expect(adminCatalogApiMock.setAdminProductMainImage).toHaveBeenCalledWith(
      "product-active",
      "image-detail",
      "csrf-token",
    );
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(2);
  });

  it("отправляет новый порядок image ids при перемещении изображения", async () => {
    const user = userEvent.setup();
    await renderPanel();
    const card = screen.getByRole("article", { name: /detail\.jpg/ });

    await user.click(within(card).getByRole("button", { name: "Выше" }));

    expect(adminCatalogApiMock.updateAdminProductImageOrder).toHaveBeenCalledWith(
      "product-active",
      ["image-detail", "image-main"],
      "csrf-token",
    );
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(2);
  });

  it("удаляет изображение и обновляет список", async () => {
    const user = userEvent.setup();
    await renderPanel();
    const card = screen.getByRole("article", { name: /detail\.jpg/ });

    await user.click(within(card).getByRole("button", { name: "Удалить" }));

    expect(adminCatalogApiMock.deleteAdminProductImage).toHaveBeenCalledWith(
      "product-active",
      "image-detail",
      "csrf-token",
    );
    await waitFor(() => expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(2));
  });

  it("не запрашивает изображения без выбранного товара", () => {
    render(<AdminProductImagesPanel productId={null} csrfToken="csrf-token" />);

    expect(adminCatalogApiMock.getAdminProductImages).not.toHaveBeenCalled();
    expect(screen.getByText("Выберите товар.")).toBeInTheDocument();
  });

  it("не обновляет старый товар после upload, если выбран уже другой товар", async () => {
    const user = userEvent.setup();
    const uploadRequest = deferred<AdminProductImagesResponse>();
    const nextImagesRequest = deferred<AdminProductImagesResponse>();
    adminCatalogApiMock.uploadAdminProductImages.mockReturnValueOnce(uploadRequest.promise);
    adminCatalogApiMock.getAdminProductImages
      .mockResolvedValueOnce(imagesResponse([mainImage]))
      .mockReturnValueOnce(nextImagesRequest.promise);

    const { rerender } = render(<AdminProductImagesPanel productId="product-active" csrfToken="csrf-token" />);
    await screen.findByRole("article", { name: /main\.jpg/ });

    await user.upload(screen.getByLabelText("Загрузить изображения"), [
      new File(["next"], "next-upload.jpg", { type: "image/jpeg" }),
    ]);
    rerender(<AdminProductImagesPanel productId="product-next" csrfToken="csrf-token" />);

    expect(screen.getByLabelText("Загрузить изображения")).toBeEnabled();

    await act(async () => {
      uploadRequest.resolve(imagesResponse([mainImage, detailImage]));
      nextImagesRequest.resolve(imagesResponse([nextProductImage]));
    });

    expect(await screen.findByRole("article", { name: /next\.jpg/ })).toBeInTheDocument();
    expect(screen.queryByRole("article", { name: /main\.jpg/ })).not.toBeInTheDocument();
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(2);
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenNthCalledWith(1, "product-active");
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenNthCalledWith(2, "product-next");
  });

  it("resets pending upload state when switching away and back to a product", async () => {
    const user = userEvent.setup();
    const uploadRequest = deferred<AdminProductImagesResponse>();
    adminCatalogApiMock.uploadAdminProductImages.mockReturnValueOnce(uploadRequest.promise);
    adminCatalogApiMock.getAdminProductImages.mockResolvedValue(imagesResponse([mainImage]));

    const { rerender } = render(<AdminProductImagesPanel productId="product-active" csrfToken="csrf-token" />);
    await screen.findByRole("article", { name: /main\.jpg/ });

    await user.upload(document.querySelector('input[type="file"]') as HTMLInputElement, [
      new File(["pending"], "pending-upload.jpg", { type: "image/jpeg" }),
    ]);
    expect(document.querySelector('input[type="file"]')).toBeDisabled();

    rerender(<AdminProductImagesPanel productId="product-next" csrfToken="csrf-token" />);
    await waitFor(() => expect(document.querySelector('input[type="file"]')).toBeEnabled());

    rerender(<AdminProductImagesPanel productId="product-active" csrfToken="csrf-token" />);
    await waitFor(() => expect(document.querySelector('input[type="file"]')).toBeEnabled());

    await act(async () => {
      uploadRequest.resolve(imagesResponse([mainImage, detailImage]));
    });
  });

  it("не обновляет изображения старого товара после image mutation, если панель размонтирована", async () => {
    const user = userEvent.setup();
    const mainRequest = deferred<AdminProductImage>();
    adminCatalogApiMock.setAdminProductMainImage.mockReturnValueOnce(mainRequest.promise);

    const { unmount } = render(<AdminProductImagesPanel productId="product-active" csrfToken="csrf-token" />);
    await screen.findByRole("article", { name: /main\.jpg/ });

    await user.click(within(screen.getByRole("article", { name: /detail\.jpg/ })).getByRole("button", { name: "Сделать основным" }));
    unmount();
    mainRequest.resolve(detailImage);

    await waitFor(() => expect(adminCatalogApiMock.setAdminProductMainImage).toHaveBeenCalledWith(
      "product-active",
      "image-detail",
      "csrf-token",
    ));
    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledTimes(1);
  });
});
