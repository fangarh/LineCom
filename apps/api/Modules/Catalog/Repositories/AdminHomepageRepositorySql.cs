namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminHomepageRepositorySql
{
    public const string SectionExists = """
        SELECT EXISTS (
            SELECT 1
            FROM homepage_sections section
            WHERE section.id = @SectionId
        );
        """;

    public const string UpdateSection = """
        UPDATE homepage_sections
        SET
            title = COALESCE(@Title, title),
            item_limit = COALESCE(@ItemLimit, item_limit),
            sort_order = COALESCE(@SortOrder, sort_order),
            is_active = COALESCE(@IsActive, is_active)
        WHERE id = @SectionId
        RETURNING
            id AS "Id",
            code AS "Code",
            title AS "Title",
            type AS "Type",
            item_limit AS "ItemLimit",
            sort_order AS "SortOrder",
            is_active AS "IsActive";
        """;

    public const string InsertSectionItem = """
        WITH existing AS (
            SELECT
                id,
                section_id,
                product_id,
                category_id,
                sort_order,
                is_active
            FROM homepage_section_items
            WHERE section_id = @SectionId
                AND (
                    (@ProductId IS NOT NULL AND product_id = @ProductId)
                    OR (@CategoryId IS NOT NULL AND category_id = @CategoryId)
                )
        ),
        inserted AS (
            INSERT INTO homepage_section_items (
                section_id,
                product_id,
                category_id,
                sort_order,
                is_active
            )
            SELECT
                @SectionId,
                @ProductId,
                @CategoryId,
                COALESCE(@SortOrder, 0),
                COALESCE(@IsActive, TRUE)
            WHERE EXISTS (
                SELECT 1
                FROM homepage_sections section
                WHERE section.id = @SectionId
            )
                AND num_nonnulls(@ProductId, @CategoryId) = 1
                AND NOT EXISTS (SELECT 1 FROM existing)
            RETURNING
                id,
                section_id,
                product_id,
                category_id,
                sort_order,
                is_active
        )
        SELECT
            id AS "Id",
            product_id AS "ProductId",
            category_id AS "CategoryId",
            sort_order AS "SortOrder",
            is_active AS "IsActive"
        FROM inserted
        WHERE num_nonnulls(product_id, category_id) = 1
        UNION ALL
        SELECT
            id AS "Id",
            product_id AS "ProductId",
            category_id AS "CategoryId",
            sort_order AS "SortOrder",
            is_active AS "IsActive"
        FROM existing
        WHERE num_nonnulls(product_id, category_id) = 1;
        """;

    public const string UpdateSectionItem = """
        UPDATE homepage_section_items
        SET
            sort_order = COALESCE(@SortOrder, sort_order),
            is_active = COALESCE(@IsActive, is_active)
        WHERE section_id = @SectionId
            AND id = @ItemId
        RETURNING
            id AS "Id",
            product_id AS "ProductId",
            category_id AS "CategoryId",
            sort_order AS "SortOrder",
            is_active AS "IsActive";
        """;

    public const string UpdateSectionItemOrder = """
        WITH requested_items AS (
            SELECT item_id, sort_order
            FROM unnest(CAST(@ItemIds AS uuid[])) WITH ORDINALITY AS item_order(item_id, sort_order)
        ),
        valid_section_items AS (
            SELECT requested_items.item_id, requested_items.sort_order
            FROM requested_items
            INNER JOIN homepage_section_items item ON item.id = requested_items.item_id
                AND item.section_id = @SectionId
        ),
        updated AS (
            UPDATE homepage_section_items item
            SET sort_order = valid_section_items.sort_order::int
            FROM valid_section_items
            WHERE item.section_id = @SectionId
                AND item.id = valid_section_items.item_id
                AND (SELECT COUNT(*) FROM valid_section_items) = (SELECT COUNT(*) FROM requested_items)
            RETURNING item.id
        )
        SELECT id
        FROM updated;
        """;

    public const string DeleteSectionItem = """
        DELETE FROM homepage_section_items
        WHERE section_id = @SectionId
            AND id = @ItemId
        RETURNING id;
        """;
}
