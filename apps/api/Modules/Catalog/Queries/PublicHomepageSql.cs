namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicHomepageSql
{
    public const string GetSections = """
        SELECT
            section.id AS "Id",
            section.code AS "Code",
            section.title AS "Title",
            section.type AS "Type",
            section.item_limit AS "ItemLimit"
        FROM homepage_sections section
        WHERE section.is_active = TRUE
        ORDER BY section.sort_order, section.code;
        """;

    public const string GetSectionItems = """
        SELECT
            item.id AS "Id",
            item.section_id AS "SectionId",
            item.product_id AS "ProductId",
            item.category_id AS "CategoryId",
            COALESCE(product.name, category.name) AS "Name",
            COALESCE(product.slug, category.slug) AS "Slug",
            COALESCE(product.sku, product_category.name) AS "SecondaryText",
            item.sort_order AS "SortOrder"
        FROM homepage_section_items item
        LEFT JOIN products product ON product.id = item.product_id
        LEFT JOIN categories product_category ON product_category.id = product.primary_category_id
        LEFT JOIN categories category ON category.id = item.category_id
        WHERE item.is_active = TRUE
            AND (
                (
                    item.product_id IS NOT NULL
                    AND product.id IS NOT NULL
                    AND product.is_active = TRUE
                    AND product.publish_status = 'published'
                    AND product.slug IS NOT NULL
                    AND product_category.is_active = TRUE
                )
                OR (
                    item.category_id IS NOT NULL
                    AND category.id IS NOT NULL
                    AND category.is_active = TRUE
                    AND category.slug IS NOT NULL
                )
            )
        ORDER BY item.section_id, item.sort_order, item.id;
        """;
}
