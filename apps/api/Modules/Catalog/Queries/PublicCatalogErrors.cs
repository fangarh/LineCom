using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicCatalogErrors
{
    public static ApiException CategoryNotFound()
    {
        return new ApiException(
            "catalog.category_not_found",
            "Категория не найдена.",
            StatusCodes.Status404NotFound);
    }

    public static ApiException ProductNotFound()
    {
        return new ApiException(
            "catalog.product_not_found",
            "Товар не найден.",
            StatusCodes.Status404NotFound);
    }

    public static ApiException InvalidPagination()
    {
        return new ApiException(
            "catalog.invalid_pagination",
            "Некорректные параметры пагинации.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException InvalidSort()
    {
        return new ApiException(
            "catalog.invalid_sort",
            "Некорректный параметр сортировки.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException InvalidFilter()
    {
        return new ApiException(
            "catalog.invalid_filter",
            "Некорректный параметр фильтра.",
            StatusCodes.Status400BadRequest);
    }
}
