# Admin Request Processing v1 design

## Status

Approved for planning on 2026-05-10.

This document fixes the first release slice of the LineCom admin request-processing workflow.

## Context

LineCom already has the public catalog, customer auth, request draft flow, request submission, and customer request history. The next release value is an internal seller/admin workflow that lets staff process customer requests created through the site.

The product remains a catalog-request system, not a classic online store. Admin request processing must not introduce public prices, online payment, invoices, shipping, quotation files, order fixation, or 1C integration.

## Goals

- Give sellers and admins a shared queue of all customer requests.
- Let staff inspect a request with customer, organization, product snapshots, customer comment, current internal comment, and history.
- Let staff change request status.
- Let staff maintain one current internal comment on the request.
- Record status and internal-comment changes in request history.
- Keep customer-facing request history isolated to the customer's own requests.

## Non-goals

- Editing request items.
- Public or internal prices.
- Invoice, payment, shipment, legal order, or quotation-file workflows.
- Email/SMS notifications.
- Assigning a request to a specific manager.
- 1C synchronization.
- Admin catalog management.
- A feed of separate internal notes.

## Status Model

The release request statuses are:

| Code | Label |
| --- | --- |
| `new` | Новая |
| `in_progress` | В работе |
| `completed` | Завершена |
| `cancelled` | Отменена |

The existing `quoted` status is removed from the release model. A migration maps existing `quoted` values to `in_progress` before tightening database constraints.

Migration rules:

- `requests.status = 'quoted'` becomes `in_progress`.
- `request_history.old_status = 'quoted'` becomes `in_progress`.
- `request_history.new_status = 'quoted'` becomes `in_progress`.
- Constraints for `requests.status`, `request_history.old_status`, and `request_history.new_status` are recreated with only the four release statuses.
- Backend request reference data is updated to remove `quoted`.
- Human-readable project docs are updated so they no longer describe `quoted` as part of the release workflow.

## Authorization

Admin request endpoints are available only to active authenticated users with role `seller` or `admin`.

Access behavior:

- No auth returns `401 auth.unauthorized`.
- Inactive authenticated user returns `403 auth.user_inactive`.
- Authenticated `customer` returns `403 auth.forbidden`.
- `seller` and `admin` can read and process all requests.

Admin routes do not assign requests to a manager. All sellers and admins work with the same shared request queue.

## Backend API

Base route:

```text
/api/admin/requests
```

### GET `/api/admin/requests`

Returns a paged shared list of requests for sellers and admins.

Query parameters:

| Query | Type | Rule |
| --- | --- | --- |
| `page` | integer | Minimum `1`, default `1`. |
| `pageSize` | integer | `1..60`, default `20`. |
| `status` | string | One of the four release statuses. |
| `number` | string | Partial request number search. |
| `contact` | string | Searches customer snapshot name, email, or phone. |
| `organization` | string | Searches organization snapshot name or INN. |

Sort order:

```text
created_at desc, number desc
```

Response:

```json
{
  "items": [
    {
      "number": "ЗК26-0008",
      "status": { "code": "new", "label": "Новая" },
      "source": "cart",
      "itemsCount": 3,
      "customer": {
        "name": "Иван Петров",
        "email": "ivan@example.com",
        "phone": "+79000000000"
      },
      "organization": {
        "name": "ООО Сеть",
        "inn": "7700000000",
        "contactPerson": "Иван Петров"
      },
      "customerComment": "Нужна консультация по срокам.",
      "internalComment": "Позвонить после 15:00.",
      "createdAt": "2026-05-10T12:40:00Z",
      "updatedAt": "2026-05-10T13:10:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

### GET `/api/admin/requests/{number}`

Returns a full request card by public request number.

The response contains:

- number;
- status;
- source;
- customer snapshot;
- organization snapshot, if present;
- customer comment;
- current internal comment;
- created and updated timestamps;
- item snapshots;
- request history.

Request items keep the same no-price rule as customer-facing request DTOs.

### PATCH `/api/admin/requests/{number}/status`

Updates request status and writes a history event.

Request:

```json
{
  "status": "in_progress"
}
```

Rules:

- Status must be one of `new`, `in_progress`, `completed`, `cancelled`.
- Updating to the current status is an idempotent no-op and returns the current admin request detail.
- A real status change writes a `status_changed` history event with old and new status.
- The acting user is stored in history through `request_history.actor_user_id`.
- The request `updated_at` timestamp changes when status changes.

Response returns the updated admin request detail.

### PUT `/api/admin/requests/{number}/internal-comment`

Replaces the current internal comment and writes a history event when the value changes.

Request:

```json
{
  "internalComment": "Позвонить клиенту после 15:00. Уточнить замену второй позиции."
}
```

Rules:

- The comment is staff-only and never appears in public catalog pages.
- Empty or whitespace input clears the internal comment.
- No separate internal-note feed is created in v1.
- A changed comment writes a `comment_added` history event with a staff-facing message. Customer-facing request endpoints do not return internal-comment history messages.
- The request `updated_at` timestamp changes when the internal comment changes.

Response returns the updated admin request detail.

## Data Model

Use the existing `requests`, `request_items`, and `request_history` tables.

Expected data support:

- `requests.internal_comment` stores the one current internal comment. If the current schema does not have this column, add it with a SQL migration.
- `request_history` stores created/status/comment events. If it already has old/new status fields, reuse them for status changes.
- `request_history.actor_user_id` stores the staff user who performed an admin action. The column is nullable so existing `created` events and system-created records remain valid.

No JSONB is introduced for request processing.

No request item data is rewritten during admin processing. Product, customer, and organization snapshots remain historical snapshots from request creation.

## Frontend UI

Admin UI uses the existing Next.js frontend and restrained B2B visual language.

### `/admin/requests`

Purpose: shared working queue and triage.

Screen contents:

- admin shell/navigation;
- status filter for all four statuses;
- filters for request number, customer contact, and organization;
- paged request list;
- each request summary shows number, status, created date, customer, organization, item count, customer comment preview, and internal comment preview when present;
- controlled empty state;
- controlled auth/forbidden states.

### `/admin/requests/[number]`

Purpose: request processing.

Screen contents:

- back link to request list;
- request number, status, created/updated timestamps;
- customer snapshot;
- organization snapshot;
- customer comment;
- item snapshots with sale unit and quantity;
- history;
- right-side processing panel on desktop, stacked action panel on mobile;
- status selector;
- current internal-comment textarea;
- separate save actions for status and internal comment.

The chosen internal-comment design is one current editable text field, not a feed of separate internal notes.

## Frontend Access Behavior

- `/admin/*` requires auth.
- Unauthenticated users are sent to login with `returnTo`.
- Authenticated `customer` users see a controlled forbidden state or are routed away from admin screens.
- `seller` and `admin` can use the screens.
- Mutating admin requests send `X-CSRF-Token`.

## Error Handling

Use existing `ApiErrorResponse`.

Expected errors:

| HTTP | Code | Meaning |
| --- | --- | --- |
| `400` | `validation.invalid_request` | Bad query/body shape. |
| `400` | `request.invalid_status` | Status is not one of the four release statuses. |
| `401` | `auth.unauthorized` | User is not authenticated. |
| `403` | `auth.forbidden` | Authenticated user lacks `seller/admin` role or CSRF failed on mutation. |
| `403` | `auth.user_inactive` | Authenticated user is inactive. |
| `404` | `request.not_found` | Request number does not exist. |

Internal exception messages must not reach API responses.

## Tests

Backend tests:

- migration maps `quoted` to `in_progress` and tightens constraints;
- `RequestReferenceData` rejects `quoted`;
- admin list rejects unauthenticated and customer users;
- admin list allows seller/admin users;
- admin list filters by status, number, contact, and organization;
- admin detail returns snapshots, internal comment, items, and history;
- status update validates four-status model;
- status update writes history;
- internal-comment update writes current comment and history;
- customer account request endpoints do not expose admin-only internal comment or staff-only comment history.

Frontend tests:

- admin list renders filters and request summaries;
- admin detail renders snapshots, items, history, status control, and internal-comment textarea;
- customer role cannot use admin pages;
- unauthenticated user is sent to login with `returnTo`;
- status update sends CSRF and refreshes visible state;
- internal-comment update sends CSRF and refreshes visible state;
- no public price, payment, invoice, shipment, or order wording is introduced.

Browser QA:

- `/admin/requests` desktop and mobile;
- `/admin/requests/{number}` desktop and mobile;
- forbidden state as customer;
- login redirect when unauthenticated;
- status update flow;
- internal-comment update flow;
- no horizontal overflow and no blank pages.

## Documentation Updates

Update human-readable project docs after implementation:

- `vault/Человекочитаемое/Продуктовая модель.md` if needed for the four-status model.
- `vault/Человекочитаемое/Auth Request Core API.md` or a new `Admin Request Processing API.md` for admin contract.
- Add an iterations note for this admin slice before or during implementation.

## SEO/GEO Impact

Admin pages are authenticated internal pages and are not indexable public catalog content. They must not alter public catalog URLs, category/product metadata, canonical behavior, sitemap behavior, or public product content.

The four-status request model has no SEO/GEO effect.

## Technical Debt Check

This design intentionally avoids temporary shortcuts:

- `quoted` is removed across database constraints, backend reference data, API behavior, frontend UI, and docs instead of hidden only in the UI.
- Admin access is role-based and does not rely on frontend-only hiding.
- Internal comments have a single explicit v1 model instead of an ambiguous partial notes system.
- Public prices and order/payment workflow remain out of scope.
- The implementation must update tests and human-readable docs before the slice is closed.
