namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicCategorySql
{
    public const string GetActiveCategories = """
        SELECT
            id AS "Id",
            parent_id AS "ParentId",
            name AS "Name",
            slug AS "Slug",
            h1 AS "H1",
            description AS "Description",
            sort_order AS "SortOrder",
            is_visible_in_menu AS "IsVisibleInMenu"
        FROM categories
        WHERE is_active = TRUE
        ORDER BY parent_id NULLS FIRST, sort_order, name, slug;
        """;

    public const string GetActiveCategoryBreadcrumbs = """
        WITH RECURSIVE category_breadcrumbs AS (
            SELECT
                id AS "Id",
                parent_id AS "ParentId",
                name AS "Name",
                slug AS "Slug",
                description AS "Description",
                h1 AS "H1",
                seo_title AS "SeoTitle",
                seo_description AS "SeoDescription",
                0 AS "Depth",
                ARRAY[id] AS "Path"
            FROM categories
            WHERE slug = @Slug
                AND is_active = TRUE

            UNION ALL

            SELECT
                parent.id AS "Id",
                parent.parent_id AS "ParentId",
                parent.name AS "Name",
                parent.slug AS "Slug",
                parent.description AS "Description",
                parent.h1 AS "H1",
                parent.seo_title AS "SeoTitle",
                parent.seo_description AS "SeoDescription",
                child."Depth" + 1 AS "Depth",
                child."Path" || parent.id AS "Path"
            FROM categories parent
            INNER JOIN category_breadcrumbs child ON child."ParentId" = parent.id
            WHERE parent.is_active = TRUE
                AND NOT parent.id = ANY(child."Path")
        )
        SELECT
            "Id",
            "ParentId",
            "Name",
            "Slug",
            "Description",
            "H1",
            "SeoTitle",
            "SeoDescription",
            "Depth"
        FROM category_breadcrumbs
        ORDER BY "Depth" DESC;
        """;

    public const string GetActiveCategoryFilters = """
        SELECT
            name AS "Name",
            slug AS "Slug"
        FROM categories
        WHERE slug = @Slug
            AND is_active = TRUE;

        SELECT
            attribute.code AS "Code",
            attribute.name AS "Name",
            attribute.type AS "Type",
            attribute.unit AS "Unit",
            attribute.sort_order AS "SortOrder",
            option.value AS "OptionValue",
            option.slug AS "OptionSlug",
            option.sort_order AS "OptionSortOrder"
        FROM categories category
        INNER JOIN category_attributes attribute ON attribute.category_id = category.id
            AND attribute.is_active = TRUE
            AND attribute.is_filterable = TRUE
        LEFT JOIN attribute_options option ON option.attribute_id = attribute.id
            AND attribute.type = 'select'
            AND option.is_active = TRUE
        WHERE category.slug = @Slug
            AND category.is_active = TRUE
        ORDER BY
            attribute.sort_order,
            attribute.name,
            attribute.code,
            option.sort_order NULLS FIRST,
            option.value NULLS FIRST,
            option.slug NULLS FIRST;
        """;

    public const string GetActiveCatalogFilters = """
        SELECT
            attribute.code AS "Code",
            MIN(attribute.name) AS "Name",
            attribute.type AS "Type",
            MIN(attribute.unit) AS "Unit",
            MIN(attribute.sort_order) AS "SortOrder",
            MIN(option.value) AS "OptionValue",
            option.slug AS "OptionSlug",
            MIN(option.sort_order) AS "OptionSortOrder"
        FROM category_attributes attribute
        INNER JOIN categories category ON category.id = attribute.category_id
            AND category.is_active = TRUE
        LEFT JOIN attribute_options option ON option.attribute_id = attribute.id
            AND attribute.type = 'select'
            AND option.is_active = TRUE
        WHERE attribute.is_active = TRUE
            AND attribute.is_filterable = TRUE
        GROUP BY
            attribute.code,
            attribute.type,
            option.slug
        ORDER BY
            MIN(attribute.sort_order),
            MIN(attribute.name),
            attribute.code,
            MIN(option.sort_order) NULLS FIRST,
            MIN(option.value) NULLS FIRST,
            option.slug NULLS FIRST;
        """;
}
