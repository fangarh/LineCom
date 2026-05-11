namespace LineCom.Api.Modules.Catalog.Queries;

internal static class AdminProductDuplicateSql
{
    public const string FindCandidates = """
        SELECT
            product.id AS "Id",
            product.name AS "Name",
            product.slug AS "Slug",
            product.sku AS "Sku",
            product.external_id AS "ExternalId",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            brand.name AS "BrandName",
            product.publish_status AS "PublishStatus",
            product.is_active AS "IsActive",
            CASE
                WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 1
                WHEN @ExternalId IS NOT NULL AND product.external_id = @ExternalId THEN 1
                WHEN @Slug IS NOT NULL AND product.slug = @Slug THEN 1
                ELSE COALESCE(GREATEST(similarity(product.name, @Name), similarity(product.slug, @Slug)), 0)
            END::numeric AS "Similarity"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
        LEFT JOIN brands brand ON brand.id = product.brand_id
        WHERE (@ExcludeProductId IS NULL OR product.id <> @ExcludeProductId)
            AND (
                (@Sku IS NOT NULL AND product.sku = @Sku)
                OR (@ExternalId IS NOT NULL AND product.external_id = @ExternalId)
                OR (@Slug IS NOT NULL AND product.slug = @Slug)
                OR (
                    @Name IS NOT NULL
                    AND product.primary_category_id = @CategoryId
                    AND similarity(product.name, @Name) >= @SimilarityThreshold
                )
                OR (
                    @Name IS NOT NULL
                    AND @BrandId IS NOT NULL
                    AND product.brand_id = @BrandId
                    AND similarity(product.name, @Name) >= @SimilarityThreshold
                )
                OR (
                    @Slug IS NOT NULL
                    AND similarity(product.slug, @Slug) >= @SimilarityThreshold
                )
            )
        ORDER BY
            CASE
                WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 0
                WHEN @ExternalId IS NOT NULL AND product.external_id = @ExternalId THEN 1
                WHEN @Slug IS NOT NULL AND product.slug = @Slug THEN 2
                ELSE 3
            END,
            CASE
                WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 1
                WHEN @ExternalId IS NOT NULL AND product.external_id = @ExternalId THEN 1
                WHEN @Slug IS NOT NULL AND product.slug = @Slug THEN 1
                ELSE COALESCE(GREATEST(similarity(product.name, @Name), similarity(product.slug, @Slug)), 0)
            END DESC,
            product.name,
            product.slug
        LIMIT @Limit;
        """;
}
