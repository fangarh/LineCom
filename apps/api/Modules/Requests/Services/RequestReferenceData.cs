using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Requests.Services;

public interface IRequestReferenceData
{
    RequestStatusDto GetStatus(string code);
}

public sealed class RequestReferenceData : IRequestReferenceData
{
    private static readonly IReadOnlyDictionary<string, string> StatusLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["new"] = "Новая",
            ["in_progress"] = "В работе",
            ["quoted"] = "КП отправлено",
            ["completed"] = "Завершена",
            ["cancelled"] = "Отменена"
        };

    public RequestStatusDto GetStatus(string code)
    {
        if (StatusLabels.TryGetValue(code, out var label))
        {
            return new RequestStatusDto(code, label);
        }

        throw new ApiException(
            "request.invalid_status",
            "Некорректный статус заявки.",
            StatusCodes.Status400BadRequest);
    }
}
