namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminCatalogProductSql
{
    private const string ProductFilters = """
        WHERE (@CategoryId IS NULL OR product.primary_category_id = @CategoryId)
            AND (@BrandId IS NULL OR product.brand_id = @BrandId)
            AND (@IsActive IS NULL OR product.is_active = @IsActive)
            AND (@PublishStatus IS NULL OR product.publish_status = @PublishStatus)
            AND (
                @Search IS NULL
                OR product.name ILIKE '%' || @Search || '%'
                OR product.slug ILIKE '%' || @Search || '%'
                OR product.sku ILIKE '%' || @Search || '%'
                OR product.external_id ILIKE '%' || @Search || '%'
                OR category.name ILIKE '%' || @Search || '%'
                OR brand.name ILIKE '%' || @Search || '%'
            )
        """;

    public const string CountProducts = $$"""
        SELECT COUNT(*)::int
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
        LEFT JOIN brands brand ON brand.id = product.brand_id
        {{ProductFilters}};
        """;

    public const string ListProducts = $$"""
        SELECT
            product.id AS "Id",
            product.name AS "Name",
            product.slug AS "Slug",
            product.sku AS "Sku",
            product.external_id AS "ExternalId",
            category.id AS "CategoryId",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            category.is_active AS "CategoryIsActive",
            brand.id AS "BrandId",
            brand.name AS "BrandName",
            product.publish_status AS "PublishStatus",
            product.is_active AS "IsActive",
            product.availability_status AS "AvailabilityStatus",
            product.sort_order AS "SortOrder",
            readiness."MissingRequiredAttributeCount" AS "MissingRequiredAttributeCount",
            readiness."InvalidAttributeValueCount" AS "InvalidAttributeValueCount"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
        LEFT JOIN brands brand ON brand.id = product.brand_id
        LEFT JOIN LATERAL (
            SELECT
                COUNT(required_attribute.id) FILTER (WHERE value.id IS NULL)::int AS "MissingRequiredAttributeCount",
                COUNT(value.id) FILTER (
                    WHERE NOT (
                        (attribute.type = 'text' AND value.value_text IS NOT NULL)
                        OR (attribute.type = 'number' AND value.value_number IS NOT NULL)
                        OR (attribute.type = 'boolean' AND value.value_boolean IS NOT NULL)
                        OR (attribute.type = 'select' AND value.attribute_option_id IS NOT NULL)
                    )
                )::int AS "InvalidAttributeValueCount"
            FROM category_attributes attribute
            LEFT JOIN category_attributes required_attribute ON required_attribute.id = attribute.id
                AND required_attribute.is_required = TRUE
                AND required_attribute.is_active = TRUE
            LEFT JOIN product_attribute_values value ON value.attribute_id = attribute.id
                AND value.product_id = product.id
            WHERE attribute.category_id = product.primary_category_id
                AND attribute.is_active = TRUE
        ) readiness ON TRUE
        {{ProductFilters}}
        ORDER BY category.name, product.sort_order, product.name, product.slug
        LIMIT @PageSize OFFSET @Offset;
        """;

    public const string GetProduct = """
        SELECT
            product.id AS "Id",
            product.primary_category_id AS "CategoryId",
            category.name AS "CategoryName",
            category.is_active AS "CategoryIsActive",
            brand.id AS "BrandId",
            brand.name AS "BrandName",
            product.name AS "Name",
            product.slug AS "Slug",
            product.sku AS "Sku",
            product.external_id AS "ExternalId",
            product.description AS "Description",
            product.short_description AS "ShortDescription",
            product.availability_status AS "AvailabilityStatus",
            product.sale_unit AS "SaleUnit",
            product.unit_quantity AS "UnitQuantity",
            product.publish_status AS "PublishStatus",
            product.is_active AS "IsActive",
            product.seo_title AS "SeoTitle",
            product.seo_description AS "SeoDescription",
            product.h1 AS "H1",
            product.sort_order AS "SortOrder",
            image_summary."ImagesCount" AS "ImagesCount",
            image_summary."MainImageFileId" AS "MainImageFileId",
            readiness."MissingRequiredAttributeCount" AS "MissingRequiredAttributeCount",
            readiness."InvalidAttributeValueCount" AS "InvalidAttributeValueCount"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
        LEFT JOIN brands brand ON brand.id = product.brand_id
        LEFT JOIN LATERAL (
            SELECT
                COUNT(image.id)::int AS "ImagesCount",
                MAX(image.stored_file_id) FILTER (WHERE image.is_main) AS "MainImageFileId"
            FROM product_images image
            INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
                AND stored_file.status = 'active'
                AND stored_file.purpose = 'product_image'
            WHERE image.product_id = product.id
        ) image_summary ON TRUE
        LEFT JOIN LATERAL (
            SELECT
                COUNT(required_attribute.id) FILTER (WHERE value.id IS NULL)::int AS "MissingRequiredAttributeCount",
                COUNT(value.id) FILTER (
                    WHERE NOT (
                        (attribute.type = 'text' AND value.value_text IS NOT NULL)
                        OR (attribute.type = 'number' AND value.value_number IS NOT NULL)
                        OR (attribute.type = 'boolean' AND value.value_boolean IS NOT NULL)
                        OR (attribute.type = 'select' AND value.attribute_option_id IS NOT NULL)
                    )
                )::int AS "InvalidAttributeValueCount"
            FROM category_attributes attribute
            LEFT JOIN category_attributes required_attribute ON required_attribute.id = attribute.id
                AND required_attribute.is_required = TRUE
                AND required_attribute.is_active = TRUE
            LEFT JOIN product_attribute_values value ON value.attribute_id = attribute.id
                AND value.product_id = product.id
            WHERE attribute.category_id = product.primary_category_id
                AND attribute.is_active = TRUE
        ) readiness ON TRUE
        WHERE product.id = @Id;
        """;

    public const string GetProductAttributes = """
        SELECT
            attribute.id AS "AttributeId",
            attribute.code AS "Code",
            attribute.name AS "Name",
            attribute.type AS "Type",
            attribute.unit AS "Unit",
            value.value_text AS "ValueText",
            value.value_number AS "ValueNumber",
            value.value_boolean AS "ValueBoolean",
            value.attribute_option_id AS "AttributeOptionId",
            option.value AS "OptionValue",
            attribute.is_required AS "IsRequired",
            (
                (attribute.type = 'text' AND value.value_text IS NOT NULL)
                OR (attribute.type = 'number' AND value.value_number IS NOT NULL)
                OR (attribute.type = 'boolean' AND value.value_boolean IS NOT NULL)
                OR (attribute.type = 'select' AND value.attribute_option_id IS NOT NULL AND option.id IS NOT NULL)
            ) AS "IsValidValue"
        FROM product_attribute_values value
        INNER JOIN category_attributes attribute ON attribute.id = value.attribute_id
        LEFT JOIN attribute_options option ON option.id = value.attribute_option_id
        WHERE value.product_id = @Id
        ORDER BY attribute.sort_order, attribute.name, attribute.code;
        """;

    public const string FindDuplicateHardIdentity = """
        SELECT
            product.id AS "ProductId",
            CASE
                WHEN product.slug = @Slug THEN 'slug'
                WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 'sku'
                ELSE 'external_id'
            END AS "Field"
        FROM products product
        WHERE (@ExcludeProductId IS NULL OR product.id <> @ExcludeProductId)
            AND (
                product.slug = @Slug
                OR (@Sku IS NOT NULL AND product.sku = @Sku)
                OR (@ExternalId IS NOT NULL AND product.external_id = @ExternalId)
            )
        ORDER BY
            CASE
                WHEN product.slug = @Slug THEN 0
                WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 1
                ELSE 2
            END
        LIMIT 1;
        """;

    public const string GetReadinessCategory = """
        SELECT
            TRUE AS "CategoryExists",
            category.is_active AS "CategoryIsActive"
        FROM categories category
        WHERE category.id = @CategoryId;
        """;

    public const string GetReadinessRequiredAttributes = """
        SELECT
            attribute.id AS "AttributeId",
            attribute.code AS "Code",
            attribute.name AS "Name",
            attribute.type AS "Type",
            value.value_text AS "ValueText",
            value.value_number AS "ValueNumber",
            value.value_boolean AS "ValueBoolean",
            value.attribute_option_id AS "AttributeOptionId"
        FROM category_attributes attribute
        LEFT JOIN product_attribute_values value ON value.attribute_id = attribute.id
            AND value.product_id = @ProductId
        WHERE attribute.category_id = @CategoryId
            AND attribute.is_active = TRUE
            AND attribute.is_required = TRUE
        ORDER BY attribute.sort_order, attribute.name, attribute.code;
        """;

    public const string CountInvalidAttributeValues = """
        SELECT COUNT(value.id)::int
        FROM product_attribute_values value
        INNER JOIN category_attributes attribute ON attribute.id = value.attribute_id
        LEFT JOIN attribute_options option ON option.id = value.attribute_option_id
        WHERE value.product_id = @ProductId
            AND attribute.category_id = @CategoryId
            AND NOT (
                (attribute.type = 'text' AND value.value_text IS NOT NULL)
                OR (attribute.type = 'number' AND value.value_number IS NOT NULL)
                OR (attribute.type = 'boolean' AND value.value_boolean IS NOT NULL)
                OR (attribute.type = 'select' AND value.attribute_option_id IS NOT NULL AND option.id IS NOT NULL)
            );
        """;

    public const string InsertProduct = """
        INSERT INTO products (
            primary_category_id,
            brand_id,
            name,
            slug,
            sku,
            external_id,
            description,
            short_description,
            availability_status,
            sale_unit,
            unit_quantity,
            publish_status,
            is_active,
            seo_title,
            seo_description,
            h1,
            sort_order
        )
        VALUES (
            @CategoryId,
            @BrandId,
            @Name,
            @Slug,
            @Sku,
            @ExternalId,
            @Description,
            @ShortDescription,
            @AvailabilityStatus,
            @SaleUnit,
            @UnitQuantity,
            @PublishStatus,
            @IsActive,
            @SeoTitle,
            @SeoDescription,
            @H1,
            @SortOrder
        )
        RETURNING id;
        """;

    public const string UpdateProduct = """
        UPDATE products
        SET
            primary_category_id = @CategoryId,
            brand_id = @BrandId,
            name = @Name,
            slug = @Slug,
            sku = @Sku,
            external_id = @ExternalId,
            description = @Description,
            short_description = @ShortDescription,
            availability_status = @AvailabilityStatus,
            sale_unit = @SaleUnit,
            unit_quantity = @UnitQuantity,
            publish_status = @PublishStatus,
            is_active = @IsActive,
            seo_title = @SeoTitle,
            seo_description = @SeoDescription,
            h1 = @H1,
            sort_order = @SortOrder
        WHERE id = @Id
        RETURNING id;
        """;

    public const string CountProductUsage = """
        SELECT
            (SELECT COUNT(*)::int FROM request_items item WHERE item.product_id = @Id)
            + (SELECT COUNT(*)::int FROM homepage_section_items item WHERE item.product_id = @Id);
        """;

    public const string DeleteProduct = """
        DELETE FROM products
        WHERE id = @Id;
        """;
}
