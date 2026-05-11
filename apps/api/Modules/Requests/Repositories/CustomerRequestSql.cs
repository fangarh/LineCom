namespace LineCom.Api.Modules.Requests.Repositories;

internal static class CustomerRequestSql
{
    public const string FindOrganizationSnapshot = """
        SELECT
            id AS "Id",
            name AS "Name",
            inn AS "Inn",
            contact_person AS "ContactPerson",
            phone AS "Phone",
            email AS "Email"
        FROM organizations
        WHERE user_id = @UserId
        LIMIT 1;
        """;

    public const string FindProductSnapshots = """
        SELECT
            product.id AS "ProductId",
            product.name AS "ProductName",
            product.slug AS "ProductSlug",
            product.sku AS "ProductSku",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            brand.name AS "BrandName",
            brand.slug AS "BrandSlug",
            product.availability_status AS "AvailabilityStatus",
            product.sale_unit AS "SaleUnit",
            product.unit_quantity AS "UnitQuantity"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
            AND category.is_active = TRUE
        LEFT JOIN brands brand ON brand.id = product.brand_id
            AND brand.is_active = TRUE
        WHERE product.id = ANY(@ProductIds)
            AND product.publish_status = 'published';
        """;

    public const string InsertRequest = """
        INSERT INTO requests (
            number,
            number_year,
            number_sequence,
            user_id,
            organization_id,
            status,
            source,
            customer_name,
            customer_email,
            customer_phone,
            organization_name,
            organization_inn,
            organization_contact_person,
            organization_phone,
            organization_email,
            customer_comment
        )
        VALUES (
            @Number,
            @NumberYear,
            @NumberSequence,
            @UserId,
            @OrganizationId,
            'new',
            @Source,
            @CustomerName,
            @CustomerEmail,
            @CustomerPhone,
            @OrganizationName,
            @OrganizationInn,
            @OrganizationContactPerson,
            @OrganizationPhone,
            @OrganizationEmail,
            @CustomerComment
        )
        RETURNING
            id AS "Id",
            number AS "Number",
            status AS "Status",
            source AS "Source",
            customer_comment AS "CustomerComment",
            created_at AS "CreatedAt";
        """;

    public const string InsertRequestItem = """
        INSERT INTO request_items (
            request_id,
            product_id,
            quantity,
            product_name,
            product_slug,
            product_sku,
            category_name,
            category_slug,
            brand_name,
            brand_slug,
            availability_status,
            sale_unit,
            unit_quantity,
            customer_comment,
            sort_order
        )
        VALUES (
            @RequestId,
            @ProductId,
            @Quantity,
            @ProductName,
            @ProductSlug,
            @ProductSku,
            @CategoryName,
            @CategorySlug,
            @BrandName,
            @BrandSlug,
            @AvailabilityStatus,
            @SaleUnit,
            @UnitQuantity,
            @CustomerComment,
            @SortOrder
        );
        """;

    public const string InsertCreatedHistory = """
        INSERT INTO request_history (
            request_id,
            event_type,
            actor_user_id
        )
        VALUES (
            @RequestId,
            'created',
            @ActorUserId
        );
        """;

    public const string CountCurrentUserRequests = """
        SELECT COUNT(*)::int
        FROM requests request
        WHERE request.user_id = @UserId
            AND (@Status IS NULL OR request.status = @Status);
        """;

    public const string FindCurrentUserRequests = """
        SELECT
            request.number AS "Number",
            request.status AS "Status",
            request.source AS "Source",
            COUNT(item.id)::int AS "ItemsCount",
            request.customer_comment AS "CustomerComment",
            request.created_at AS "CreatedAt"
        FROM requests request
        LEFT JOIN request_items item ON item.request_id = request.id
        WHERE request.user_id = @UserId
            AND (@Status IS NULL OR request.status = @Status)
        GROUP BY request.id
        ORDER BY request.created_at DESC, request.number DESC
        LIMIT @PageSize
        OFFSET @Offset;
        """;

    public const string FindCurrentUserRequestDetail = """
        SELECT
            request.id AS "Id",
            request.number AS "Number",
            request.status AS "Status",
            request.source AS "Source",
            request.customer_name AS "CustomerName",
            request.customer_email AS "CustomerEmail",
            request.customer_phone AS "CustomerPhone",
            request.organization_name AS "OrganizationName",
            request.organization_inn AS "OrganizationInn",
            request.organization_contact_person AS "OrganizationContactPerson",
            request.customer_comment AS "CustomerComment",
            request.created_at AS "CreatedAt"
        FROM requests request
        WHERE request.user_id = @UserId
            AND request.number = @Number
        LIMIT 1;
        """;

    public const string FindRequestItems = """
        SELECT
            item.product_id AS "ProductId",
            item.product_name AS "ProductName",
            item.product_sku AS "ProductSku",
            item.sale_unit AS "SaleUnit",
            item.unit_quantity AS "UnitQuantity",
            item.quantity::int AS "Quantity",
            item.customer_comment AS "CustomerComment"
        FROM request_items item
        WHERE item.request_id = @RequestId
        ORDER BY item.sort_order, item.created_at;
        """;

    public const string FindRequestHistory = """
        SELECT
            history.event_type AS "Event",
            CASE history.event_type
                WHEN 'created' THEN 'Р—Р°СЏРІРєР° СЃРѕР·РґР°РЅР°.'
                ELSE history.event_type
            END AS "Message",
            history.created_at AS "CreatedAt"
        FROM request_history history
        WHERE history.request_id = @RequestId
            AND history.event_type IN ('created', 'status_changed')
        ORDER BY history.created_at, history.id;
        """;
}
