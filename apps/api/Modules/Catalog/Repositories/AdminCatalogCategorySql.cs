namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminCatalogCategorySql
{
    public const string CountCategories = """
        SELECT COUNT(*)::int
        FROM categories category
        WHERE (@ParentId IS NULL OR category.parent_id = @ParentId)
            AND (@Search IS NULL OR category.name ILIKE '%' || @Search || '%' OR category.slug ILIKE '%' || @Search || '%')
            AND (@IsActive IS NULL OR category.is_active = @IsActive);
        """;

    public const string ListCategories = """
        SELECT
            category.id AS "Id",
            category.parent_id AS "ParentId",
            category.name AS "Name",
            category.slug AS "Slug",
            category.description AS "Description",
            category.seo_title AS "SeoTitle",
            category.seo_description AS "SeoDescription",
            category.h1 AS "H1",
            category.sort_order AS "SortOrder",
            category.is_active AS "IsActive",
            category.is_visible_in_menu AS "IsVisibleInMenu",
            COALESCE(product."ProductsCount", 0) AS "ProductsCount",
            COALESCE(child."ChildrenCount", 0) AS "ChildrenCount"
        FROM categories category
        LEFT JOIN (
            SELECT product.primary_category_id, COUNT(product.id)::int AS "ProductsCount"
            FROM products product
            GROUP BY product.primary_category_id
        ) product ON product.primary_category_id = category.id
        LEFT JOIN (
            SELECT child.parent_id, COUNT(child.id)::int AS "ChildrenCount"
            FROM categories child
            GROUP BY child.parent_id
        ) child ON child.parent_id = category.id
        WHERE (@ParentId IS NULL OR category.parent_id = @ParentId)
            AND (@Search IS NULL OR category.name ILIKE '%' || @Search || '%' OR category.slug ILIKE '%' || @Search || '%')
            AND (@IsActive IS NULL OR category.is_active = @IsActive)
        ORDER BY category.parent_id NULLS FIRST, category.sort_order, category.name
        LIMIT @PageSize OFFSET @Offset;
        """;

    public const string GetCategory = """
        SELECT
            category.id AS "Id",
            category.parent_id AS "ParentId",
            category.name AS "Name",
            category.slug AS "Slug",
            category.description AS "Description",
            category.seo_title AS "SeoTitle",
            category.seo_description AS "SeoDescription",
            category.h1 AS "H1",
            category.sort_order AS "SortOrder",
            category.is_active AS "IsActive",
            category.is_visible_in_menu AS "IsVisibleInMenu",
            COALESCE(product."ProductsCount", 0) AS "ProductsCount",
            COALESCE(child."ChildrenCount", 0) AS "ChildrenCount"
        FROM categories category
        LEFT JOIN (
            SELECT product.primary_category_id, COUNT(product.id)::int AS "ProductsCount"
            FROM products product
            GROUP BY product.primary_category_id
        ) product ON product.primary_category_id = category.id
        LEFT JOIN (
            SELECT child.parent_id, COUNT(child.id)::int AS "ChildrenCount"
            FROM categories child
            GROUP BY child.parent_id
        ) child ON child.parent_id = category.id
        WHERE category.id = @Id
        ;
        """;

    public const string InsertCategory = """
        WITH inserted AS (
            INSERT INTO categories (
                parent_id,
                name,
                slug,
                description,
                seo_title,
                seo_description,
                h1,
                sort_order,
                is_active,
                is_visible_in_menu
            )
            VALUES (
                @ParentId,
                @Name,
                @Slug,
                @Description,
                @SeoTitle,
                @SeoDescription,
                @H1,
                @SortOrder,
                @IsActive,
                @IsVisibleInMenu
            )
            RETURNING
                id,
                parent_id,
                name,
                slug,
                description,
                seo_title,
                seo_description,
                h1,
                sort_order,
                is_active,
                is_visible_in_menu
        )
        SELECT
            inserted.id AS "Id",
            inserted.parent_id AS "ParentId",
            inserted.name AS "Name",
            inserted.slug AS "Slug",
            inserted.description AS "Description",
            inserted.seo_title AS "SeoTitle",
            inserted.seo_description AS "SeoDescription",
            inserted.h1 AS "H1",
            inserted.sort_order AS "SortOrder",
            inserted.is_active AS "IsActive",
            inserted.is_visible_in_menu AS "IsVisibleInMenu",
            0 AS "ProductsCount",
            0 AS "ChildrenCount"
        FROM inserted;
        """;

    public const string UpdateCategory = """
        UPDATE categories
        SET
            parent_id = @ParentId,
            name = @Name,
            slug = @Slug,
            description = @Description,
            seo_title = @SeoTitle,
            seo_description = @SeoDescription,
            h1 = @H1,
            sort_order = @SortOrder,
            is_active = @IsActive,
            is_visible_in_menu = @IsVisibleInMenu
        WHERE id = @Id
        RETURNING id;
        """;

    public const string MoveCategory = """
        UPDATE categories
        SET parent_id = @ParentId
        WHERE id = @Id
        RETURNING id;
        """;

    public const string SortCategory = """
        UPDATE categories
        SET sort_order = @SortOrder
        WHERE id = @Id
        RETURNING id;
        """;

    public const string CountCategoryUsage = """
        SELECT
            (
                SELECT COUNT(*)::int FROM categories child WHERE child.parent_id = @Id
            )
            + (
                SELECT COUNT(*)::int FROM products product WHERE product.primary_category_id = @Id
            )
            + (
                SELECT COUNT(*)::int FROM homepage_section_items item WHERE item.category_id = @Id
            );
        """;

    public const string DeleteCategory = """
        DELETE FROM categories
        WHERE id = @Id;
        """;
}
