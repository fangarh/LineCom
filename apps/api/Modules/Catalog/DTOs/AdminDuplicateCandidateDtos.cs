namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminProductDuplicateCandidatesResponse(
    IReadOnlyList<AdminProductDuplicateCandidateDto> Items);

public sealed record AdminProductDuplicateCandidateDto(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? ExternalId,
    string CategoryName,
    string CategorySlug,
    string? BrandName,
    string PublishStatus,
    bool IsActive,
    decimal Similarity);
