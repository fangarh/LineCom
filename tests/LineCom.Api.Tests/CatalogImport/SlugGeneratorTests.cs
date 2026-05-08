using LineCom.CatalogImport.Core.Planning;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class SlugGeneratorTests
{
    [Fact]
    public void CreateUniqueSlug_TransliteratesRussianAndKeepsTechnicalTokens()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var slug = SlugGenerator.CreateUniqueSlug("Кабель LANMAX UTP4 cat.5e, 305m, Cu", used);

        Assert.Equal("kabel-lanmax-utp4-cat-5e-305m-cu", slug);
    }

    [Fact]
    public void CreateUniqueSlug_AppendsSuffixForCollisions()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kabel-lanmax-utp4-cat-5e",
            "kabel-lanmax-utp4-cat-5e-2"
        };

        var slug = SlugGenerator.CreateUniqueSlug("Кабель LANMAX UTP4 cat.5e", used);

        Assert.Equal("kabel-lanmax-utp4-cat-5e-3", slug);
    }
}
