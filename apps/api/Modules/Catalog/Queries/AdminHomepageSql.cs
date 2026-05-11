namespace LineCom.Api.Modules.Catalog.Queries;

internal static class AdminHomepageSql
{
    public const string GetSections = """
        SELECT
            id AS "Id",
            code AS "Code",
            title AS "Title",
            type AS "Type",
            item_limit AS "ItemLimit",
            sort_order AS "SortOrder",
            is_active AS "IsActive"
        FROM homepage_sections
        ORDER BY sort_order, code;
        """;

    public const string GetSectionItems = """
        SELECT
            item.id AS "Id",
            item.section_id AS "SectionId",
            item.product_id AS "ProductId",
            item.category_id AS "CategoryId",
            product.name AS "ProductName",
            product.slug AS "ProductSlug",
            product.sku AS "ProductSku",
            product.is_active AS "ProductIsActive",
            product.publish_status AS "ProductPublishStatus",
            product_category.name AS "ProductCategoryName",
            product_category.is_active AS "ProductCategoryIsActive",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            category.is_active AS "CategoryIsActive",
            item.sort_order AS "SortOrder",
            item.is_active AS "IsActive"
        FROM homepage_section_items item
        LEFT JOIN products product ON product.id = item.product_id
        LEFT JOIN categories product_category ON product_category.id = product.primary_category_id
        LEFT JOIN categories category ON category.id = item.category_id
        ORDER BY item.section_id, item.sort_order, item.id;
        """;
}
