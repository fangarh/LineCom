namespace LineCom.Api.Modules.Catalog.Repositories;

internal sealed record AdminHomepageItemTarget(Guid? ProductId, Guid? CategoryId);

internal sealed record AdminHomepageSectionMutationResult(
    Guid Id,
    string Code,
    string Title,
    string Type,
    int ItemLimit,
    int SortOrder,
    bool IsActive);

internal sealed record AdminHomepageSectionItemMutationResult(
    Guid Id,
    Guid? ProductId,
    Guid? CategoryId,
    int SortOrder,
    bool IsActive);

internal sealed record AdminHomepageSectionItemOrderMutationResult(Guid Id);

internal sealed record AdminHomepageSectionItemDeleteMutationResult(Guid Id);
