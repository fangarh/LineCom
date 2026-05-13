# Account Navigation + Request Quick View Design

## Status

Approved for planning on 2026-05-13.

This document fixes the next LineCom frontend/backend slice after the existing auth, customer request, and admin request-processing flows.

## Context

LineCom already has:

- customer auth through HTTP-only cookie sessions;
- customer profile and organization pages;
- customer request draft, submission, list, and detail pages;
- admin request list and detail pages for `seller` and `admin`;
- admin catalog and homepage-management routes.

The current UX gap is discoverability and session visibility. After login the header still behaves like an anonymous header, logout is not implemented as a backend operation, and request lists require a full page transition to inspect a request.

## Goals

- Show the current user in the site header after login and after page reload when the cookie session is still active.
- Provide a visible logout action that clears the server cookie session, not only frontend runtime state.
- Make account and admin areas reachable from role-aware navigation.
- Add quick request preview from both customer and admin request lists.
- Keep customer and staff request data separated by existing backend contracts.

## Non-goals

- Editing request items.
- Assigning a manager.
- Notifications.
- Public or internal prices.
- Payment, invoice, shipment, legal order, quotation-file, or 1C workflows.
- Replacing existing customer/admin request detail pages.

## Selected Approach

Use a role-aware account navigation model.

The header becomes the primary post-login entry point:

- anonymous users see `Войти`;
- authenticated users see their display name and `Выйти`;
- customer links remain visible for authenticated users;
- `seller` and `admin` users additionally see an administration group.

This keeps the existing route split:

- customer area stays under `/account/*`;
- admin area stays under `/admin/*`;
- quick preview reuses the correct detail API for each area.

Rejected alternatives:

- Adding all links directly to the top-level header would be faster, but the header would keep growing as admin screens grow.
- A single unified `/requests` center would look compact, but it would mix customer and staff workflows that are already cleanly separated in API and routes.

## Routes And Navigation

Existing routes remain authoritative:

- `/account/profile` - profile and organization.
- `/account/requests` - customer's own requests.
- `/account/requests/[number]` - customer request detail.
- `/admin/requests` - staff request queue.
- `/admin/requests/[number]` - staff request processing.
- `/admin/catalog` - admin catalog.
- `/admin/homepage` - homepage management.

Header behavior:

- Anonymous: public navigation, request draft link, `Войти`.
- `customer`: public navigation, request draft link, `Мои заявки`, `Профиль`, display name, `Выйти`.
- `seller` or `admin`: customer navigation plus an `Администрирование` group with `Заявки клиентов`, `Каталог`, and `Главная`.

The exact desktop/mobile presentation can follow the current `SiteHeader` patterns. The important rule is that admin links are not shown to `customer` users and backend authorization remains the source of truth.

## Logout Contract

Add `POST /api/auth/logout`.

Rules:

- endpoint requires an active authenticated user;
- endpoint requires `X-CSRF-Token`;
- endpoint signs out the cookie authentication scheme;
- endpoint clears `linecom_auth`;
- response can be `204 No Content`;
- unauthenticated users receive `401 auth.unauthorized`;
- missing or invalid CSRF receives `403 auth.forbidden`.

Frontend logout behavior:

- call `POST /api/auth/logout` with current CSRF token;
- after success, clear `AuthProvider` state;
- redirect to `/` or keep the user on the current public route;
- if logout fails because the session is already expired, clear frontend state and show anonymous header.

## Auth State In Header

`AuthProvider` should support restoring the current session from `GET /api/auth/me`.

Requirements:

- successful login/register still sets `user` and `csrfToken` immediately;
- page reload with a valid cookie refreshes `user` and `csrfToken` from `GET /api/auth/me`;
- `auth.unauthorized` during session restore leaves the user anonymous without showing a page-level error;
- `auth.user_inactive` clears runtime auth state and lets protected pages show their existing controlled error/redirect behavior;
- header must not expose raw cookie data.

## Customer Quick Preview

On `/account/requests`, each request list card gets a quick preview action.

Behavior:

- clicking quick preview opens a drawer or side panel over the list;
- the panel loads `GET /api/account/requests/{number}`;
- the panel shows number, status, created date, customer comment, item snapshots, and customer-visible history;
- internal comments and staff-only history never appear;
- the panel has `Открыть полностью`, linking to `/account/requests/[number]`;
- `request.not_found` displays a controlled inline error and lets the user close the panel.

The existing full detail page remains the canonical deep link and refresh-safe view.

## Admin Quick Preview

On `/admin/requests`, each request list card gets a quick preview action.

Behavior:

- clicking quick preview opens a drawer or side panel over the staff queue;
- the panel loads `GET /api/admin/requests/{number}`;
- the panel shows number, status, created/updated dates, customer snapshot, organization snapshot, customer comment, internal comment, items, and staff-visible history;
- the panel has `Открыть обработку`, linking to `/admin/requests/[number]`;
- status and internal-comment editing remain on the full detail page for this slice.

Keeping mutations out of the drawer avoids two competing editing surfaces and keeps the first quick-preview iteration focused.

## Data Separation

The quick preview must not introduce a shared request DTO.

Customer preview uses customer endpoints:

- `GET /api/account/requests`;
- `GET /api/account/requests/{number}`.

Admin preview uses admin endpoints:

- `GET /api/admin/requests`;
- `GET /api/admin/requests/{number}`.

This preserves existing security boundaries:

- customers can only see their own requests;
- customers never receive `internalComment`;
- `seller` and `admin` can see the staff request queue;
- backend authorization remains mandatory even if frontend navigation hides links.

## UI States

Header:

- anonymous;
- restoring session;
- authenticated customer;
- authenticated seller/admin;
- logout pending;
- logout failed or expired session.

Quick preview:

- closed;
- loading;
- loaded;
- controlled backend error;
- not found;
- forbidden;
- close and reopen without losing the list filters.

## Tests

Backend tests:

- `POST /api/auth/logout` requires auth;
- logout requires CSRF;
- logout clears `linecom_auth`;
- logout returns `204 No Content` on success.

Frontend tests:

- header shows `Войти` when anonymous;
- header shows user name and `Выйти` after session restore;
- customer does not see admin navigation;
- seller/admin sees admin navigation;
- logout calls API with CSRF and clears auth state;
- customer request list opens quick preview using account detail API;
- customer preview does not render internal comment;
- admin request list opens quick preview using admin detail API;
- admin preview renders internal comment and links to full processing;
- preview close keeps current filters/list state.

Browser QA:

- desktop and mobile header anonymous/authenticated states;
- customer `Мои заявки` quick preview;
- admin `Заявки клиентов` quick preview;
- logout from an authenticated session;
- no blank pages and no horizontal overflow.

## SEO/GEO Impact

This slice changes authenticated internal UX only. It must not change public catalog URLs, product/category metadata, sitemap behavior, robots behavior, canonical links, or public catalog content.

## Technical Debt Check

The design avoids intentional shortcuts:

- logout is server-side and CSRF-protected instead of frontend-only state clearing;
- admin visibility in the header is role-based but not relied on for security;
- quick preview reuses existing detail endpoints and does not duplicate request authorization logic;
- customer and admin DTOs remain separate;
- request mutations stay on the existing full admin processing page.
