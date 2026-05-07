using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Requests.Services;

internal static class RequestErrors
{
    public static ApiException InvalidItems()
    {
        return new ApiException(
            "request.invalid_items",
            "Некорректный состав заявки.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException ProductNotAvailable()
    {
        return new ApiException(
            "request.product_not_available",
            "Товар недоступен для заявки.",
            StatusCodes.Status400BadRequest);
    }
    public static ApiException NotFound()
    {
        return new ApiException(
            "request.not_found",
            "Р—Р°СЏРІРєР° РЅРµ РЅР°Р№РґРµРЅР°.",
            StatusCodes.Status404NotFound);
    }
}
