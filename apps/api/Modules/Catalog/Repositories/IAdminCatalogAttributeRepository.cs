namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminCategoryAttributeRecord(
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
    int ProductValuesCount);

public sealed record AdminAttributeOptionRecord(
    Guid Id,
    Guid AttributeId,
    string Value,
    string Slug,
    string NormalizedValue,
    int SortOrder,
    bool IsActive,
    int ProductValuesCount);

public sealed record AdminCategoryAttributeUpsert(
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
    bool IsActive);

public sealed record AdminAttributeOptionUpsert(
    string Value,
    string Slug,
    string NormalizedValue,
    int SortOrder,
    bool IsActive);

public sealed record AdminCategoryAttributeInheritanceResult(int Added, int Skipped);

internal sealed class AdminCatalogAttributeDuplicateException : Exception
{
    public AdminCatalogAttributeDuplicateException(Exception? innerException = null)
        : base("Category attribute or option already exists.", innerException)
    {
    }
}

internal sealed class AdminCatalogAttributeInUseException : Exception
{
    public AdminCatalogAttributeInUseException(Exception? innerException = null)
        : base("Category attribute or option is in use.", innerException)
    {
    }
}

internal sealed class InvalidAdminCatalogAttributeException : Exception
{
    public InvalidAdminCatalogAttributeException(Exception? innerException = null)
        : base("Category attribute request is invalid.", innerException)
    {
    }
}

public interface IAdminCatalogAttributeRepository
{
    Task<IReadOnlyList<AdminCategoryAttributeRecord>> GetAttributesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAttributeOptionRecord>> GetOptionsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryAttributeRecord?> GetAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryAttributeRecord> CreateAttributeAsync(
        Guid categoryId,
        AdminCategoryAttributeUpsert command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryAttributeRecord?> UpdateAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        AdminCategoryAttributeUpsert command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default);

    Task<AdminAttributeOptionRecord?> GetOptionAsync(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken = default);

    Task<AdminAttributeOptionRecord> CreateOptionAsync(
        Guid categoryId,
        Guid attributeId,
        AdminAttributeOptionUpsert command,
        CancellationToken cancellationToken = default);

    Task<AdminAttributeOptionRecord?> UpdateOptionAsync(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        AdminAttributeOptionUpsert command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteOptionAsync(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryAttributeInheritanceResult> InheritFromParentAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
