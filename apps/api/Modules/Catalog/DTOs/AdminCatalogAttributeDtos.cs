namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminCategoryAttributesResponse(IReadOnlyList<AdminCategoryAttributeDto> Items);

public sealed record AdminCategoryAttributeDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Code,
    string Type,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsVisibleInProduct,
    bool IsSeoImportant,
    bool IsUsedInGeneratedName,
    int SortOrder,
    bool IsActive,
    int ProductValuesCount,
    IReadOnlyList<AdminAttributeOptionDto> Options);

public sealed record AdminAttributeOptionDto(
    Guid Id,
    string Value,
    string Slug,
    string NormalizedValue,
    int SortOrder,
    bool IsActive,
    int ProductValuesCount);

public sealed record UpsertAdminCategoryAttributeCommand(
    string? Name,
    string? Code,
    string? Type,
    string? Unit,
    bool? IsRequired,
    bool? IsFilterable,
    bool? IsComparable,
    bool? IsVisibleInProduct,
    bool? IsSeoImportant,
    bool? IsUsedInGeneratedName,
    int? SortOrder,
    bool? IsActive);

public sealed record UpsertAdminAttributeOptionCommand(
    string? Value,
    string? Slug,
    string? NormalizedValue,
    int? SortOrder,
    bool? IsActive);

public sealed record InheritAdminCategoryAttributesResponse(int Added, int Skipped);
