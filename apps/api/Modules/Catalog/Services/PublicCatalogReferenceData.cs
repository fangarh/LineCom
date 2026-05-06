using System.Collections.ObjectModel;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class PublicCatalogReferenceData : IPublicCatalogReferenceData
{
    private static readonly IReadOnlyDictionary<string, string> AvailabilityLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["in_stock"] = "В наличии",
            ["on_order"] = "Под заказ",
            ["check_availability"] = "Уточнить"
        };

    private static readonly IReadOnlyDictionary<string, string> SaleUnitLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["coil"] = "бухта",
            ["box"] = "коробка",
            ["piece"] = "штука",
            ["pack"] = "упаковка"
        };

    public IReadOnlyCollection<string> AvailabilityStatusCodes { get; } =
        new ReadOnlyCollection<string>(AvailabilityLabels.Keys.ToArray());

    public IReadOnlyCollection<string> SaleUnitCodes { get; } =
        new ReadOnlyCollection<string>(SaleUnitLabels.Keys.ToArray());

    public PublicCodeLabelDto GetAvailability(string code)
    {
        return GetCodeLabel(AvailabilityLabels, code);
    }

    public PublicCodeLabelDto GetSaleUnit(string code)
    {
        return GetCodeLabel(SaleUnitLabels, code);
    }

    private static PublicCodeLabelDto GetCodeLabel(
        IReadOnlyDictionary<string, string> labels,
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw InvalidFilterException();
        }

        if (labels.TryGetValue(code, out var label))
        {
            return new PublicCodeLabelDto(code, label);
        }

        throw InvalidFilterException();
    }

    private static ApiException InvalidFilterException()
    {
        return new ApiException(
            "catalog.invalid_filter",
            "Некорректный параметр фильтра.",
            StatusCodes.Status400BadRequest);
    }
}
