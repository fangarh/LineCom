using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicCategoryDetailBuilderTests
{
    [Fact]
    public void Build_ReturnsCategoryDetailWithSeoAndBreadcrumbs()
    {
        var rootId = Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1");
        var childId = Guid.Parse("dcd4f577-6076-4283-b256-30ea0822a3b2");

        var detail = PublicCategoryDetailBuilder.Build(
        [
            CreateRow(rootId, null, "Витая пара", "vitaya-para", depth: 1),
            CreateRow(
                childId,
                rootId,
                "Кабель U/UTP",
                "u-utp",
                depth: 0,
                seoTitle: "Кабель U/UTP купить",
                seoDescription: "Каталог кабеля U/UTP.")
        ]);

        Assert.Equal(childId, detail.Id);
        Assert.Equal(rootId, detail.ParentId);
        Assert.Equal("Кабель U/UTP", detail.Name);
        Assert.Equal("u-utp", detail.Slug);
        Assert.Equal("Кабель U/UTP купить", detail.Seo.Title);
        Assert.Equal("Каталог кабеля U/UTP.", detail.Seo.Description);
        Assert.Equal("/catalog/u-utp", detail.Seo.CanonicalPath);
        Assert.Equal(["vitaya-para", "u-utp"], detail.Breadcrumbs.Select(item => item.Slug).ToArray());
    }

    [Fact]
    public void Build_ThrowsCategoryNotFound_WhenRowsAreEmpty()
    {
        var exception = Assert.Throws<ApiException>(() => PublicCategoryDetailBuilder.Build([]));

        Assert.Equal("catalog.category_not_found", exception.Code);
        Assert.Equal("Категория не найдена.", exception.Message);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [Fact]
    public void Build_ThrowsCategoryNotFound_WhenTargetDepthRowIsMissing()
    {
        var row = CreateRow(
            Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1"),
            null,
            "Витая пара",
            "vitaya-para",
            depth: 1);

        var exception = Assert.Throws<ApiException>(() => PublicCategoryDetailBuilder.Build([row]));

        Assert.Equal("catalog.category_not_found", exception.Code);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    private static PublicCategoryDetailRow CreateRow(
        Guid id,
        Guid? parentId,
        string name,
        string slug,
        int depth,
        string? seoTitle = null,
        string? seoDescription = null)
    {
        return new PublicCategoryDetailRow(
            id,
            parentId,
            name,
            slug,
            Description: "Описание категории.",
            H1: name,
            SeoTitle: seoTitle,
            SeoDescription: seoDescription,
            Depth: depth);
    }
}
