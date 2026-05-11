namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicProductSql
{
    public const string SelectActiveCategoryIdBySlug = """
        SELECT id
        FROM categories
        WHERE slug = @CategorySlug
            AND is_active = TRUE;
        """;

    public static string BuildProductListSql(string whereSql, string orderBySql)
    {
        return $$"""
            SELECT COUNT(*)
            FROM products product
            INNER JOIN categories category ON category.id = product.primary_category_id
                AND category.is_active = TRUE
            LEFT JOIN brands brand ON brand.id = product.brand_id
                AND brand.is_active = TRUE
            WHERE product.is_active = TRUE
                AND product.publish_status = 'published'
                {{whereSql}};

            SELECT
                product.id AS "Id",
                product.name AS "Name",
                product.slug AS "Slug",
                product.sku AS "Sku",
                brand.name AS "BrandName",
                brand.slug AS "BrandSlug",
                category.name AS "CategoryName",
                category.slug AS "CategorySlug",
                product.availability_status AS "AvailabilityStatus",
                product.sale_unit AS "SaleUnit",
                product.unit_quantity AS "UnitQuantity",
                main_image.url AS "MainImageUrl",
                main_image.alt AS "MainImageAlt",
                main_image.title AS "MainImageTitle"
            FROM products product
            INNER JOIN categories category ON category.id = product.primary_category_id
                AND category.is_active = TRUE
            LEFT JOIN brands brand ON brand.id = product.brand_id
                AND brand.is_active = TRUE
            LEFT JOIN LATERAL (
                SELECT
                    '/' || stored_file.storage_key AS url,
                    image.alt,
                    image.title
                FROM product_images image
                INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
                    AND stored_file.status = 'active'
                    AND stored_file.purpose = 'product_image'
                WHERE image.product_id = product.id
                ORDER BY image.is_main DESC, image.sort_order, image.id
                LIMIT 1
            ) main_image ON TRUE
            WHERE product.is_active = TRUE
                AND product.publish_status = 'published'
                {{whereSql}}
            {{orderBySql}}
            OFFSET @Offset
            LIMIT @PageSize;
            """;
    }

    public static string BuildSelectAttributeFilterSql(int index)
    {
        return $$"""
            AND EXISTS (
                SELECT 1
                FROM product_attribute_values attribute_value
                INNER JOIN category_attributes attribute ON attribute.id = attribute_value.attribute_id
                    AND attribute.category_id = product.primary_category_id
                    AND attribute.is_active = TRUE
                    AND attribute.is_filterable = TRUE
                    AND attribute.type = 'select'
                INNER JOIN attribute_options option ON option.id = attribute_value.attribute_option_id
                    AND option.is_active = TRUE
                WHERE attribute_value.product_id = product.id
                    AND attribute.code = @AttributeCode{{index}}
                    AND option.slug = @AttributeOptionSlug{{index}}
            )
            """;
    }

    public const string GetProductDetail = """
        SELECT
            product.id AS "Id",
            product.name AS "Name",
            product.slug AS "Slug",
            product.sku AS "Sku",
            product.description AS "Description",
            product.short_description AS "ShortDescription",
            product.h1 AS "H1",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            brand.name AS "BrandName",
            brand.slug AS "BrandSlug",
            product.availability_status AS "AvailabilityStatus",
            product.sale_unit AS "SaleUnit",
            product.unit_quantity AS "UnitQuantity",
            product.seo_title AS "SeoTitle",
            product.seo_description AS "SeoDescription"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
            AND category.is_active = TRUE
        LEFT JOIN brands brand ON brand.id = product.brand_id
            AND brand.is_active = TRUE
        WHERE product.slug = @Slug
            AND product.is_active = TRUE
            AND product.publish_status = 'published';

        SELECT
            '/' || stored_file.storage_key AS "Url",
            image.alt AS "Alt",
            image.title AS "Title"
        FROM products product
        INNER JOIN product_images image ON image.product_id = product.id
        INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
        INNER JOIN categories category ON category.id = product.primary_category_id
            AND category.is_active = TRUE
        WHERE product.slug = @Slug
            AND product.is_active = TRUE
            AND product.publish_status = 'published'
        ORDER BY image.is_main DESC, image.sort_order, image.id;

        SELECT
            attribute.code AS "Code",
            attribute.name AS "Name",
            attribute.type AS "Type",
            attribute.unit AS "Unit",
            value.value_text AS "ValueText",
            value.value_number AS "ValueNumber",
            value.value_boolean AS "ValueBoolean",
            option.value AS "OptionValue",
            attribute.sort_order AS "SortOrder"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
            AND category.is_active = TRUE
        INNER JOIN product_attribute_values value ON value.product_id = product.id
        INNER JOIN category_attributes attribute ON attribute.id = value.attribute_id
            AND attribute.is_active = TRUE
            AND attribute.is_visible_in_product = TRUE
        LEFT JOIN attribute_options option ON option.id = value.attribute_option_id
            AND option.is_active = TRUE
        WHERE product.slug = @Slug
            AND product.is_active = TRUE
            AND product.publish_status = 'published'
            AND (attribute.type <> 'select' OR option.id IS NOT NULL)
        ORDER BY attribute.sort_order, attribute.name, attribute.code;

        WITH RECURSIVE product_category_breadcrumbs AS (
            SELECT
                category.id AS "Id",
                category.parent_id AS "ParentId",
                category.name AS "Name",
                category.slug AS "Slug",
                0 AS "Depth",
                ARRAY[category.id] AS "Path"
            FROM products product
            INNER JOIN categories category ON category.id = product.primary_category_id
                AND category.is_active = TRUE
            WHERE product.slug = @Slug
                AND product.is_active = TRUE
                AND product.publish_status = 'published'

            UNION ALL

            SELECT
                parent.id AS "Id",
                parent.parent_id AS "ParentId",
                parent.name AS "Name",
                parent.slug AS "Slug",
                child."Depth" + 1 AS "Depth",
                child."Path" || parent.id AS "Path"
            FROM categories parent
            INNER JOIN product_category_breadcrumbs child ON child."ParentId" = parent.id
            WHERE parent.is_active = TRUE
                AND NOT parent.id = ANY(child."Path")
        )
        SELECT
            "Name",
            "Slug",
            "Depth"
        FROM product_category_breadcrumbs
        ORDER BY "Depth" DESC;
        """;
}
