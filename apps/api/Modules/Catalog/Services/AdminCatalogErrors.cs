using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

internal static class AdminCatalogErrors
{
    public static ApiException InvalidRequest()
    {
        return new ApiException(
            "admin_catalog.invalid_request",
            "Некорректный запрос каталога.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException CategoryNotFound()
    {
        return new ApiException("admin_catalog.category_not_found", "Категория не найдена.", StatusCodes.Status404NotFound);
    }

    public static ApiException BrandNotFound()
    {
        return new ApiException("admin_catalog.brand_not_found", "Бренд не найден.", StatusCodes.Status404NotFound);
    }

    public static ApiException ProductNotFound()
    {
        return new ApiException("admin_catalog.product_not_found", "Товар не найден.", StatusCodes.Status404NotFound);
    }

    public static ApiException ImageNotFound()
    {
        return new ApiException(
            "admin_catalog.image_not_found",
            "Изображение не найдено.",
            StatusCodes.Status404NotFound);
    }

    public static ApiException InvalidImageType()
    {
        return new ApiException(
            "admin_catalog.invalid_image_type",
            "Изображение имеет недопустимый тип.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException ImageTooLarge()
    {
        return new ApiException(
            "admin_catalog.image_too_large",
            "Изображение превышает допустимый размер.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException ImageOrderMismatch()
    {
        return new ApiException(
            "admin_catalog.image_order_mismatch",
            "Порядок изображений не соответствует изображениям товара.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException SlugAlreadyExists()
    {
        return new ApiException("admin_catalog.slug_already_exists", "Slug уже используется.", StatusCodes.Status409Conflict);
    }

    public static ApiException SkuAlreadyExists()
    {
        return new ApiException("admin_catalog.sku_already_exists", "SKU уже используется.", StatusCodes.Status409Conflict);
    }

    public static ApiException ExternalIdAlreadyExists()
    {
        return new ApiException("admin_catalog.external_id_already_exists", "ExternalId уже используется.", StatusCodes.Status409Conflict);
    }

    public static ApiException EntityInUse(string message)
    {
        return new ApiException("admin_catalog.entity_in_use", message, StatusCodes.Status409Conflict);
    }

    public static ApiException ProductNotReady()
    {
        return new ApiException(
            "admin_catalog.product_not_ready",
            "\u0422\u043e\u0432\u0430\u0440 \u043d\u0435 \u0433\u043e\u0442\u043e\u0432 \u043a \u043f\u0443\u0431\u043b\u0438\u043a\u0430\u0446\u0438\u0438.",
            StatusCodes.Status409Conflict);
    }
}
