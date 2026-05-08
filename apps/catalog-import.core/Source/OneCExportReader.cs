using System.Text.Json;

namespace LineCom.CatalogImport.Core.Source;

public static class OneCExportReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static OneCExportDocument Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Source path is required.", nameof(path));
        }

        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<OneCExportDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException("1C export JSON is empty or invalid.");

        Validate(document);

        return document;
    }

    private static void Validate(OneCExportDocument document)
    {
        if (document.Extraction is null)
        {
            throw new InvalidOperationException("Extraction section is required.");
        }

        if (string.IsNullOrWhiteSpace(document.Extraction.SourceAccount))
        {
            throw new InvalidOperationException("Extraction sourceAccount is required.");
        }

        if (document.Source is null)
        {
            throw new InvalidOperationException("Source section is required.");
        }

        if (document.Categories is null)
        {
            throw new InvalidOperationException("Categories array is required.");
        }

        if (document.Categories.Count == 0)
        {
            throw new InvalidOperationException("At least one category is required.");
        }

        foreach (var category in document.Categories)
        {
            if (category is null)
            {
                throw new InvalidOperationException("Category entry is required.");
            }

            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                throw new InvalidOperationException("Category slug is required.");
            }

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new InvalidOperationException($"Category '{category.Slug}' name is required.");
            }

            if (category.Items is null)
            {
                throw new InvalidOperationException($"Category '{category.Slug}' does not contain an items array.");
            }

            foreach (var item in category.Items)
            {
                if (item is null)
                {
                    throw new InvalidOperationException($"Category '{category.Slug}' contains an empty item entry.");
                }

                if (item.SourceRow <= 0)
                {
                    throw new InvalidOperationException($"Category '{category.Slug}' contains an item without sourceRow.");
                }

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} has empty name.");
                }

                if (item.Classification is null)
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} classification is required.");
                }

                if (string.IsNullOrWhiteSpace(item.Classification.CategorySlug))
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} classification categorySlug is required.");
                }

                if (string.IsNullOrWhiteSpace(item.Classification.CategoryName))
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} classification categoryName is required.");
                }

                if (string.IsNullOrWhiteSpace(item.Classification.Confidence))
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} classification confidence is required.");
                }

                if (item.Classification.MatchedKeywords is null)
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} classification matchedKeywords array is required.");
                }
            }
        }
    }
}
