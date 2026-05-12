namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminCatalogBrandSql
{
    public const string CountBrands = """
        SELECT COUNT(*)::int
        FROM brands brand
        WHERE (@Search IS NULL OR brand.name ILIKE '%' || @Search || '%' OR brand.slug ILIKE '%' || @Search || '%')
            AND (@IsActive IS NULL OR brand.is_active = @IsActive);
        """;

    public const string ListBrands = """
        SELECT
            brand.id AS "Id",
            brand.name AS "Name",
            brand.slug AS "Slug",
            brand.description AS "Description",
            brand.seo_title AS "SeoTitle",
            brand.seo_description AS "SeoDescription",
            brand.logo_file_id AS "LogoFileId",
            brand.is_active AS "IsActive",
            COALESCE(product."ProductsCount", 0) AS "ProductsCount"
        FROM brands brand
        LEFT JOIN (
            SELECT product.brand_id, COUNT(product.id)::int AS "ProductsCount"
            FROM products product
            GROUP BY product.brand_id
        ) product ON product.brand_id = brand.id
        WHERE (@Search IS NULL OR brand.name ILIKE '%' || @Search || '%' OR brand.slug ILIKE '%' || @Search || '%')
            AND (@IsActive IS NULL OR brand.is_active = @IsActive)
        ORDER BY brand.name, brand.slug
        LIMIT @PageSize OFFSET @Offset;
        """;

    public const string GetBrand = """
        SELECT
            brand.id AS "Id",
            brand.name AS "Name",
            brand.slug AS "Slug",
            brand.description AS "Description",
            brand.seo_title AS "SeoTitle",
            brand.seo_description AS "SeoDescription",
            brand.logo_file_id AS "LogoFileId",
            brand.is_active AS "IsActive",
            COALESCE(product."ProductsCount", 0) AS "ProductsCount"
        FROM brands brand
        LEFT JOIN (
            SELECT product.brand_id, COUNT(product.id)::int AS "ProductsCount"
            FROM products product
            GROUP BY product.brand_id
        ) product ON product.brand_id = brand.id
        WHERE brand.id = @Id;
        """;

    public const string InsertBrand = """
        WITH inserted AS (
            INSERT INTO brands (
                name,
                slug,
                description,
                seo_title,
                seo_description,
                logo_file_id,
                is_active
            )
            VALUES (
                @Name,
                @Slug,
                @Description,
                @SeoTitle,
                @SeoDescription,
                @LogoFileId,
                @IsActive
            )
            RETURNING
                id,
                name,
                slug,
                description,
                seo_title,
                seo_description,
                logo_file_id,
                is_active
        )
        SELECT
            inserted.id AS "Id",
            inserted.name AS "Name",
            inserted.slug AS "Slug",
            inserted.description AS "Description",
            inserted.seo_title AS "SeoTitle",
            inserted.seo_description AS "SeoDescription",
            inserted.logo_file_id AS "LogoFileId",
            inserted.is_active AS "IsActive",
            0 AS "ProductsCount"
        FROM inserted;
        """;

    public const string UpdateBrand = """
        UPDATE brands
        SET
            name = @Name,
            slug = @Slug,
            description = @Description,
            seo_title = @SeoTitle,
            seo_description = @SeoDescription,
            logo_file_id = @LogoFileId,
            is_active = @IsActive
        WHERE id = @Id
        RETURNING id;
        """;

    public const string QuickCreateBrand = """
        INSERT INTO brands (
            name,
            slug,
            is_active
        )
        VALUES (
            @Name,
            @Slug,
            TRUE
        )
        RETURNING
            id AS "Id",
            name AS "Name",
            slug AS "Slug",
            description AS "Description",
            seo_title AS "SeoTitle",
            seo_description AS "SeoDescription",
            logo_file_id AS "LogoFileId",
            is_active AS "IsActive",
            0 AS "ProductsCount";
        """;

    public const string DeleteBrand = """
        DELETE FROM brands brand
        WHERE brand.id = @Id
            AND NOT EXISTS (
                SELECT 1
                FROM products product
                WHERE product.brand_id = brand.id
            );
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

    public const string GetBrandLogoFileId = """
        SELECT logo_file_id
        FROM brands
        WHERE id = @BrandId
        FOR UPDATE;
        """;

    public const string UpdateBrandLogo = """
        UPDATE brands
        SET logo_file_id = @LogoFileId
        WHERE id = @BrandId
        RETURNING logo_file_id;
        """;

    public const string ClearBrandLogo = """
        UPDATE brands
        SET logo_file_id = NULL
        WHERE id = @BrandId
        RETURNING @PreviousLogoFileId;
        """;

    public const string MarkBrandLogoDeletedIfUnreferenced = """
        UPDATE stored_files stored_file
        SET status = 'deleted'
        WHERE stored_file.id = @StoredFileId
            AND stored_file.purpose = 'brand_logo'
            AND NOT EXISTS (
                SELECT 1
                FROM brands brand
                WHERE brand.logo_file_id = stored_file.id
            );
        """;

    public const string GetBrandLogo = """
        SELECT
            stored_file.id AS "StoredFileId",
            '/' || stored_file.storage_key AS "Url",
            stored_file.original_file_name AS "OriginalFileName",
            stored_file.content_type AS "ContentType",
            stored_file.size_bytes AS "SizeBytes",
            stored_file.checksum AS "Checksum"
        FROM brands brand
        INNER JOIN stored_files stored_file ON stored_file.id = brand.logo_file_id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'brand_logo'
        WHERE brand.id = @BrandId;
        """;
}
