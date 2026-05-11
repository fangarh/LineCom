namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminCatalogAttributeSql
{
    public const string ListAttributes = """
        SELECT
            attribute.id AS "Id",
            attribute.category_id AS "CategoryId",
            attribute.name AS "Name",
            attribute.code AS "Code",
            attribute.type AS "Type",
            attribute.unit AS "Unit",
            attribute.is_required AS "IsRequired",
            attribute.is_filterable AS "IsFilterable",
            attribute.is_comparable AS "IsComparable",
            attribute.is_visible_in_product AS "IsVisibleInProduct",
            attribute.is_seo_important AS "IsSeoImportant",
            attribute.is_used_in_generated_name AS "IsUsedInGeneratedName",
            attribute.sort_order AS "SortOrder",
            attribute.is_active AS "IsActive",
            COUNT(value.id)::int AS "ProductValuesCount"
        FROM category_attributes attribute
        LEFT JOIN product_attribute_values value ON value.attribute_id = attribute.id
        WHERE attribute.category_id = @CategoryId
        GROUP BY attribute.id
        ORDER BY attribute.sort_order, attribute.name, attribute.code;
        """;

    public const string ListOptions = """
        SELECT
            option.id AS "Id",
            option.attribute_id AS "AttributeId",
            option.value AS "Value",
            option.slug AS "Slug",
            option.normalized_value AS "NormalizedValue",
            option.sort_order AS "SortOrder",
            option.is_active AS "IsActive",
            COUNT(value.id)::int AS "ProductValuesCount"
        FROM attribute_options option
        INNER JOIN category_attributes attribute ON attribute.id = option.attribute_id
        LEFT JOIN product_attribute_values value ON value.attribute_option_id = option.id
        WHERE attribute.category_id = @CategoryId
            AND attribute.type = 'select'
        GROUP BY option.id
        ORDER BY option.attribute_id, option.sort_order, option.value;
        """;

    public const string GetAttribute = """
        SELECT
            attribute.id AS "Id",
            attribute.category_id AS "CategoryId",
            attribute.name AS "Name",
            attribute.code AS "Code",
            attribute.type AS "Type",
            attribute.unit AS "Unit",
            attribute.is_required AS "IsRequired",
            attribute.is_filterable AS "IsFilterable",
            attribute.is_comparable AS "IsComparable",
            attribute.is_visible_in_product AS "IsVisibleInProduct",
            attribute.is_seo_important AS "IsSeoImportant",
            attribute.is_used_in_generated_name AS "IsUsedInGeneratedName",
            attribute.sort_order AS "SortOrder",
            attribute.is_active AS "IsActive",
            COUNT(value.id)::int AS "ProductValuesCount"
        FROM category_attributes attribute
        LEFT JOIN product_attribute_values value ON value.attribute_id = attribute.id
        WHERE attribute.category_id = @CategoryId
            AND attribute.id = @AttributeId
        GROUP BY attribute.id;
        """;

    public const string InsertAttribute = """
        INSERT INTO category_attributes (
            category_id,
            name,
            code,
            type,
            unit,
            is_required,
            is_filterable,
            is_comparable,
            is_visible_in_product,
            is_seo_important,
            is_used_in_generated_name,
            sort_order,
            is_active
        )
        VALUES (
            @CategoryId,
            @Name,
            @Code,
            @Type,
            @Unit,
            @IsRequired,
            @IsFilterable,
            @IsComparable,
            @IsVisibleInProduct,
            @IsSeoImportant,
            @IsUsedInGeneratedName,
            @SortOrder,
            @IsActive
        )
        RETURNING id;
        """;

    public const string UpdateAttribute = """
        UPDATE category_attributes
        SET
            name = @Name,
            code = @Code,
            type = @Type,
            unit = @Unit,
            is_required = @IsRequired,
            is_filterable = @IsFilterable,
            is_comparable = @IsComparable,
            is_visible_in_product = @IsVisibleInProduct,
            is_seo_important = @IsSeoImportant,
            is_used_in_generated_name = @IsUsedInGeneratedName,
            sort_order = @SortOrder,
            is_active = @IsActive
        WHERE category_id = @CategoryId
            AND id = @AttributeId
        RETURNING id;
        """;

    public const string DeleteAttribute = """
        DELETE FROM category_attributes attribute
        WHERE attribute.category_id = @CategoryId
            AND attribute.id = @AttributeId
            AND NOT EXISTS (
                SELECT 1
                FROM product_attribute_values value
                WHERE value.attribute_id = attribute.id
            );
        """;

    public const string GetOption = """
        SELECT
            option.id AS "Id",
            option.attribute_id AS "AttributeId",
            option.value AS "Value",
            option.slug AS "Slug",
            option.normalized_value AS "NormalizedValue",
            option.sort_order AS "SortOrder",
            option.is_active AS "IsActive",
            COUNT(value.id)::int AS "ProductValuesCount"
        FROM attribute_options option
        INNER JOIN category_attributes attribute ON attribute.id = option.attribute_id
        LEFT JOIN product_attribute_values value ON value.attribute_option_id = option.id
        WHERE attribute.category_id = @CategoryId
            AND option.attribute_id = @AttributeId
            AND option.id = @OptionId
        GROUP BY option.id;
        """;

    public const string InsertOption = """
        INSERT INTO attribute_options (
            attribute_id,
            value,
            slug,
            normalized_value,
            sort_order,
            is_active
        )
        SELECT
            attribute.id,
            @Value,
            @Slug,
            @NormalizedValue,
            @SortOrder,
            @IsActive
        FROM category_attributes attribute
        WHERE attribute.category_id = @CategoryId
            AND attribute.id = @AttributeId
        RETURNING id;
        """;

    public const string UpdateOption = """
        UPDATE attribute_options option
        SET
            value = @Value,
            slug = @Slug,
            normalized_value = @NormalizedValue,
            sort_order = @SortOrder,
            is_active = @IsActive
        FROM category_attributes attribute
        WHERE option.attribute_id = attribute.id
            AND attribute.category_id = @CategoryId
            AND option.attribute_id = @AttributeId
            AND option.id = @OptionId
        RETURNING option.id;
        """;

    public const string DeleteOption = """
        WITH option_to_delete AS (
            SELECT option.id
            FROM attribute_options option
            INNER JOIN category_attributes attribute ON attribute.id = option.attribute_id
            WHERE attribute.category_id = @CategoryId
                AND option.attribute_id = @AttributeId
                AND option.id = @OptionId
                AND NOT EXISTS (
                    SELECT 1
                    FROM product_attribute_values value
                    WHERE value.attribute_option_id = option.id
                )
        ),
        deleted_aliases AS (
            DELETE FROM attribute_value_aliases value_alias
            USING option_to_delete
            WHERE value_alias.option_id = option_to_delete.id
            RETURNING value_alias.id
        ),
        deleted_option AS (
            DELETE FROM attribute_options option
            USING option_to_delete
            WHERE option.id = option_to_delete.id
                AND (SELECT COUNT(*)::int FROM deleted_aliases) >= 0
            RETURNING option.id
        )
        SELECT COUNT(*)::int FROM deleted_option;
        """;

    public const string InheritMissingAttributes = """
        WITH parent_category AS (
            SELECT category.parent_id
            FROM categories category
            WHERE category.id = @CategoryId
        ),
        source_attributes AS (
            SELECT parent_attribute.*
            FROM category_attributes parent_attribute
            INNER JOIN parent_category ON parent_category.parent_id = parent_attribute.category_id
        ),
        skipped_attributes AS (
            SELECT COUNT(*)::int AS skipped
            FROM source_attributes parent_attribute
            INNER JOIN category_attributes child_attribute
                ON child_attribute.category_id = @CategoryId
                AND child_attribute.code = parent_attribute.code
        ),
        inserted_attributes AS (
            INSERT INTO category_attributes (
                category_id,
                name,
                code,
                type,
                unit,
                is_required,
                is_filterable,
                is_comparable,
                is_visible_in_product,
                is_seo_important,
                is_used_in_generated_name,
                sort_order,
                is_active
            )
            SELECT
                @CategoryId,
                parent_attribute.name,
                parent_attribute.code,
                parent_attribute.type,
                parent_attribute.unit,
                parent_attribute.is_required,
                parent_attribute.is_filterable,
                parent_attribute.is_comparable,
                parent_attribute.is_visible_in_product,
                parent_attribute.is_seo_important,
                parent_attribute.is_used_in_generated_name,
                parent_attribute.sort_order,
                parent_attribute.is_active
            FROM source_attributes parent_attribute
            WHERE NOT EXISTS (
                SELECT 1
                FROM category_attributes child_attribute
                WHERE child_attribute.category_id = @CategoryId
                    AND child_attribute.code = parent_attribute.code
            )
            RETURNING
                id AS child_attribute_id,
                code,
                type
        )
        SELECT
            parent_attribute.id AS "ParentAttributeId",
            inserted_attributes.child_attribute_id AS "ChildAttributeId",
            COALESCE((SELECT skipped FROM skipped_attributes), 0) AS "Skipped"
        FROM inserted_attributes
        INNER JOIN source_attributes parent_attribute
            ON parent_attribute.code = inserted_attributes.code
            AND parent_attribute.type = inserted_attributes.type
        UNION ALL
        SELECT
            NULL::uuid AS "ParentAttributeId",
            NULL::uuid AS "ChildAttributeId",
            COALESCE((SELECT skipped FROM skipped_attributes), 0) AS "Skipped"
        WHERE NOT EXISTS (
            SELECT 1
            FROM inserted_attributes
        );
        """;

    public const string InheritOptionsForCopiedAttributes = """
        INSERT INTO attribute_options (
            attribute_id,
            value,
            slug,
            normalized_value,
            sort_order,
            is_active
        )
        SELECT
            copied_attributes.child_attribute_id,
            parent_option.value,
            parent_option.slug,
            parent_option.normalized_value,
            parent_option.sort_order,
            parent_option.is_active
        FROM unnest(@CopiedAttributeIds, @ParentAttributeIds)
            AS copied_attributes(child_attribute_id, parent_attribute_id)
        INNER JOIN category_attributes parent_attribute
            ON parent_attribute.id = copied_attributes.parent_attribute_id
            AND parent_attribute.type = 'select'
        INNER JOIN attribute_options parent_option
            ON parent_option.attribute_id = copied_attributes.parent_attribute_id;
        """;
}
