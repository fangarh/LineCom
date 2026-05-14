using Npgsql;

namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminProductPostgresExceptionMapper
{
    public static bool TryGetDuplicateField(PostgresException exception, out string field)
    {
        if (exception.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            field = string.Empty;
            return false;
        }

        field = exception.ConstraintName switch
        {
            "ux_products_sku" => "sku",
            "ux_products_external_id" => "external_id",
            _ => "slug"
        };
        return true;
    }
}
