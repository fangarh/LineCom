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
}
