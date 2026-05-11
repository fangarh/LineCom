namespace LineCom.Api.Modules.Catalog.Services;

internal static class AdminCatalogInput
{
    public static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string RequireText(string? value)
    {
        return NormalizeText(value) ?? throw AdminCatalogErrors.InvalidRequest();
    }

    public static int NormalizePage(int? value)
    {
        return value is null or < 1 ? 1 : value.Value;
    }

    public static int NormalizePageSize(int? value)
    {
        return value is null or < 1 ? 20 : Math.Min(value.Value, 60);
    }
}
