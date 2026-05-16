namespace LineCom.CatalogImport.Core.Images;

public static class ReviewedProductImageApplySql
{
    public const string SelectProductImageState = """
        SELECT
            product.id AS "ProductId",
            product.name AS "ProductName",
            COUNT(image.id) AS "ImagesCount",
            BOOL_OR(image.is_main) AS "HasMainImage"
        FROM products product
        LEFT JOIN product_images image ON image.product_id = product.id
        WHERE product.external_id = @ExternalId
        GROUP BY product.id, product.name;
        """;

    public const string InsertStoredFile = """
        INSERT INTO stored_files (
            storage_key,
            original_file_name,
            content_type,
            size_bytes,
            checksum,
            purpose,
            status)
        VALUES (
            @StorageKey,
            @OriginalFileName,
            'image/png',
            @SizeBytes,
            @Checksum,
            'product_image',
            'active')
        ON CONFLICT (storage_key) DO NOTHING
        RETURNING id;
        """;

    public const string SelectStoredFile = """
        SELECT id
        FROM stored_files
        WHERE storage_key = @StorageKey
          AND checksum = @Checksum
          AND content_type = 'image/png'
          AND purpose = 'product_image'
          AND status = 'active';
        """;

    public const string InsertProductImage = """
        INSERT INTO product_images (
            product_id,
            stored_file_id,
            alt,
            title,
            sort_order,
            is_main)
        VALUES (
            @ProductId,
            @StoredFileId,
            @Alt,
            @Title,
            @SortOrder,
            @IsMain)
        ON CONFLICT (product_id, stored_file_id) DO NOTHING;
        """;
}

public sealed record ReviewedProductImageApplyOptions(
    string ConnectionString,
    string StorageRootPath,
    bool Apply,
    bool AllowAddToProductsWithExistingImages);

public sealed record ReviewedProductImageApplyResult(
    int Planned,
    int Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Errors);
