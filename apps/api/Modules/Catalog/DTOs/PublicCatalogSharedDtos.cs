namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record PublicSeoDto(
    string? Title,
    string? Description,
    string CanonicalPath);

public sealed record PublicBreadcrumbDto(
    string Name,
    string Slug);

public sealed record PublicCategorySummaryDto(
    string Name,
    string Slug);

public sealed record PublicBrandSummaryDto(
    string Name,
    string Slug);

public sealed record PublicCodeLabelDto(
    string Code,
    string Label);

public sealed record PublicImageDto(
    string Url,
    string Alt,
    string? Title);
