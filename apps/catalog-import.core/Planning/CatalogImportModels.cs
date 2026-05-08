namespace LineCom.CatalogImport.Core.Planning;

public sealed record CatalogImportPlan(
    CatalogImportSummary Summary,
    IReadOnlyList<CatalogCategoryImportRow> Categories,
    IReadOnlyList<CatalogProductImportRow> Products,
    IReadOnlyList<CatalogImportWarning> Warnings);

public sealed record CatalogImportSummary(
    int Categories,
    int Products,
    int PublishableProducts,
    int DraftProducts,
    int ImageAssignments,
    int Warnings);

public sealed record CatalogCategoryImportRow(
    string Slug,
    string Name,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu);

public sealed record CatalogProductImportRow(
    int SourceRow,
    string ExternalId,
    string Name,
    string Slug,
    string CategorySlug,
    string AvailabilityStatus,
    string SaleUnit,
    string UnitQuantity,
    string PublishStatus,
    int SortOrder,
    CatalogProductImageImportRow? Image);

public sealed record CatalogProductImageImportRow(
    string AssetKey,
    string File,
    string RightsStatus);

public sealed record CatalogImportWarning(
    int? SourceRow,
    string Code,
    string Message);
