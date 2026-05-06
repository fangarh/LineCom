using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IPublicCatalogReferenceData
{
    IReadOnlyCollection<string> AvailabilityStatusCodes { get; }

    IReadOnlyCollection<string> SaleUnitCodes { get; }

    PublicCodeLabelDto GetAvailability(string code);

    PublicCodeLabelDto GetSaleUnit(string code);
}
