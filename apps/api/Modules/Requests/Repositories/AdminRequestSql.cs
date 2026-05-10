namespace LineCom.Api.Modules.Requests.Repositories;

internal static class AdminRequestSql
{
    public const string CountRequests = """
        SELECT COUNT(*)::int
        FROM requests request
        WHERE (@Status IS NULL OR request.status = @Status)
            AND (@Number IS NULL OR request.number ILIKE '%' || @Number || '%')
            AND (
                @Contact IS NULL
                OR request.customer_name ILIKE '%' || @Contact || '%'
                OR request.customer_email::text ILIKE '%' || @Contact || '%'
                OR request.customer_phone::text ILIKE '%' || @Contact || '%'
            )
            AND (
                @Organization IS NULL
                OR request.organization_name ILIKE '%' || @Organization || '%'
                OR request.organization_inn ILIKE '%' || @Organization || '%'
                OR request.organization_contact_person ILIKE '%' || @Organization || '%'
                OR request.organization_phone ILIKE '%' || @Organization || '%'
                OR request.organization_email::text ILIKE '%' || @Organization || '%'
            );
        """;

    public const string FindRequests = """
        SELECT
            request.number AS "Number",
            request.status AS "Status",
            request.source AS "Source",
            COUNT(item.id)::int AS "ItemsCount",
            request.customer_name AS "CustomerName",
            request.customer_email AS "CustomerEmail",
            request.customer_phone AS "CustomerPhone",
            request.organization_name AS "OrganizationName",
            request.organization_inn AS "OrganizationInn",
            request.organization_contact_person AS "OrganizationContactPerson",
            request.customer_comment AS "CustomerComment",
            request.internal_comment AS "InternalComment",
            request.created_at AS "CreatedAt",
            request.updated_at AS "UpdatedAt"
        FROM requests request
        LEFT JOIN request_items item ON item.request_id = request.id
        WHERE (@Status IS NULL OR request.status = @Status)
            AND (@Number IS NULL OR request.number ILIKE '%' || @Number || '%')
            AND (
                @Contact IS NULL
                OR request.customer_name ILIKE '%' || @Contact || '%'
                OR request.customer_email::text ILIKE '%' || @Contact || '%'
                OR request.customer_phone::text ILIKE '%' || @Contact || '%'
            )
            AND (
                @Organization IS NULL
                OR request.organization_name ILIKE '%' || @Organization || '%'
                OR request.organization_inn ILIKE '%' || @Organization || '%'
                OR request.organization_contact_person ILIKE '%' || @Organization || '%'
                OR request.organization_phone ILIKE '%' || @Organization || '%'
                OR request.organization_email::text ILIKE '%' || @Organization || '%'
            )
        GROUP BY request.id
        ORDER BY request.created_at DESC, request.number DESC
        LIMIT @PageSize
        OFFSET @Offset;
        """;

    public const string FindRequestDetail = """
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
            request.internal_comment AS "InternalComment",
            request.created_at AS "CreatedAt",
            request.updated_at AS "UpdatedAt"
        FROM requests request
        WHERE request.number = @Number
        LIMIT 1;
        """;

    public const string FindRequestForUpdate = """
        SELECT
            request.id AS "Id",
            request.status AS "Status",
            request.internal_comment AS "InternalComment"
        FROM requests request
        WHERE request.number = @Number
        LIMIT 1
        FOR UPDATE;
        """;

    public const string UpdateStatus = """
        UPDATE requests
        SET status = @Status
        WHERE id = @RequestId;
        """;

    public const string InsertStatusChangedHistory = """
        INSERT INTO request_history (
            request_id,
            event_type,
            actor_user_id,
            old_status,
            new_status
        )
        VALUES (
            @RequestId,
            'status_changed',
            @ActorUserId,
            @OldStatus,
            @NewStatus
        );
        """;

    public const string UpdateInternalComment = """
        UPDATE requests
        SET internal_comment = @InternalComment
        WHERE id = @RequestId;
        """;

    public const string InsertInternalCommentHistory = """
        INSERT INTO request_history (
            request_id,
            event_type,
            actor_user_id,
            comment
        )
        VALUES (
            @RequestId,
            'comment_added',
            @ActorUserId,
            @InternalComment
        );
        """;

    public const string FindRequestItems = CustomerRequestSql.FindRequestItems;

    public const string FindRequestHistory = """
        SELECT
            history.event_type AS "Event",
            CASE history.event_type
                WHEN 'created' THEN 'Request created.'
                WHEN 'status_changed' THEN 'Status changed.'
                WHEN 'comment_added' THEN COALESCE(history.comment, 'Internal comment added.')
                ELSE history.event_type
            END AS "Message",
            history.created_at AS "CreatedAt"
        FROM request_history history
        WHERE history.request_id = @RequestId
            AND history.event_type IN ('created', 'status_changed', 'comment_added')
        ORDER BY history.created_at, history.id;
        """;
}
