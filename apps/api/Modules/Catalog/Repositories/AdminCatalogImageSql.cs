namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminCatalogImageSql
{
    public const string ProductExists = """
        SELECT EXISTS (
            SELECT 1
            FROM products product
            WHERE product.id = @ProductId
        );
        """;

    public const string GetProductName = """
        SELECT product.name
        FROM products product
        WHERE product.id = @ProductId;
        """;

    public const string ListProductImages = """
        SELECT
            image.id AS "Id",
            stored_file.id AS "StoredFileId",
            '/' || stored_file.storage_key AS "Url",
            stored_file.original_file_name AS "OriginalFileName",
            stored_file.content_type AS "ContentType",
            stored_file.size_bytes AS "SizeBytes",
            stored_file.checksum AS "Checksum",
            image.alt AS "Alt",
            image.title AS "Title",
            image.sort_order AS "SortOrder",
            image.is_main AS "IsMain",
            image.created_at AS "CreatedAt"
        FROM product_images image
        INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
        WHERE image.product_id = @ProductId
        ORDER BY image.is_main DESC, image.sort_order, image.id;
        """;

    public const string InsertStoredFile = """
        INSERT INTO stored_files (
            id,
            storage_key,
            original_file_name,
            content_type,
            size_bytes,
            checksum,
            purpose,
            status,
            created_by_user_id
        )
        VALUES (
            @Id,
            @StorageKey,
            @OriginalFileName,
            @ContentType,
            @SizeBytes,
            @Checksum,
            @Purpose,
            'active',
            @CreatedByUserId
        );
        """;

    public const string InsertProductImage = """
        INSERT INTO product_images (
            product_id,
            stored_file_id,
            alt,
            title,
            sort_order,
            is_main
        )
        SELECT
            @ProductId,
            @StoredFileId,
            @Alt,
            NULL,
            COALESCE(MAX(sort_order), 0) + 10,
            NOT EXISTS (
                SELECT 1
                FROM product_images existing
                INNER JOIN stored_files existing_file ON existing_file.id = existing.stored_file_id
                    AND existing_file.status = 'active'
                WHERE existing.product_id = @ProductId
            )
        FROM product_images
        WHERE product_id = @ProductId
        RETURNING id;
        """;

    public const string UpdateProductImage = """
        UPDATE product_images image
        SET
            alt = @Alt,
            title = @Title
        WHERE image.id = @ImageId
            AND image.product_id = @ProductId
        RETURNING image.id;
        """;

    public const string GetProductImageIds = """
        SELECT image.id
        FROM product_images image
        INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
        WHERE image.product_id = @ProductId
        ORDER BY image.sort_order, image.id;
        """;

    public const string UpdateProductImageSortOrder = """
        UPDATE product_images
        SET sort_order = @SortOrder
        WHERE id = @ImageId
            AND product_id = @ProductId;
        """;

    public const string ClearProductMainImages = """
        UPDATE product_images
        SET is_main = FALSE
        WHERE product_id = @ProductId
            AND is_main = TRUE;
        """;

    public const string SetProductMainImage = """
        UPDATE product_images image
        SET is_main = TRUE
        FROM stored_files stored_file
        WHERE image.stored_file_id = stored_file.id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
            AND image.id = @ImageId
            AND image.product_id = @ProductId
        RETURNING image.id;
        """;

    public const string GetProductImageForDelete = """
        SELECT
            image.id AS "Id",
            image.stored_file_id AS "StoredFileId",
            image.is_main AS "IsMain"
        FROM product_images image
        WHERE image.id = @ImageId
            AND image.product_id = @ProductId;
        """;

    public const string DeleteProductImage = """
        DELETE FROM product_images
        WHERE id = @ImageId
            AND product_id = @ProductId;
        """;

    public const string MarkStoredFileDeletedIfUnreferenced = """
        UPDATE stored_files stored_file
        SET status = 'deleted'
        WHERE stored_file.id = @StoredFileId
            AND NOT EXISTS (
                SELECT 1
                FROM product_images image
                WHERE image.stored_file_id = stored_file.id
            );
        """;

    public const string PromoteFirstRemainingProductImage = """
        UPDATE product_images image
        SET is_main = TRUE
        WHERE image.id = (
            SELECT remaining.id
            FROM product_images remaining
            INNER JOIN stored_files stored_file ON stored_file.id = remaining.stored_file_id
                AND stored_file.status = 'active'
                AND stored_file.purpose = 'product_image'
            WHERE remaining.product_id = @ProductId
            ORDER BY remaining.sort_order, remaining.id
            LIMIT 1
        )
            AND NOT EXISTS (
                SELECT 1
                FROM product_images main_image
                WHERE main_image.product_id = @ProductId
                    AND main_image.is_main = TRUE
            );
        """;
}
