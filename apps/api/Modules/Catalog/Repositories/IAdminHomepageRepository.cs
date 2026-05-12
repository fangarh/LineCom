using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Repositories;

public interface IAdminHomepageRepository
{
    Task<bool> SectionExistsAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionDto?> UpdateSectionAsync(Guid sectionId, UpdateAdminHomepageSectionCommand command, CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionItemDto?> InsertItemAsync(Guid sectionId, CreateAdminHomepageSectionItemCommand command, CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionItemDto?> UpdateItemAsync(Guid sectionId, Guid itemId, UpdateAdminHomepageSectionItemCommand command, CancellationToken cancellationToken = default);

    Task<bool> UpdateItemOrderAsync(Guid sectionId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default);

    Task<bool> DeleteItemAsync(Guid sectionId, Guid itemId, CancellationToken cancellationToken = default);
}

internal sealed class InvalidAdminHomepageMutationException : Exception
{
    public InvalidAdminHomepageMutationException(Exception? innerException = null)
        : base("Homepage mutation request is invalid.", innerException)
    {
    }
}
