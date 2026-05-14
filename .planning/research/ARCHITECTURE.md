# Research: Architecture

**Date:** 2026-05-14

## Current Architecture

LineCom is a modular monolith backend plus separate Next.js frontend and operational tools:

- ASP.NET Core API composition root: `apps/api/Program.cs`.
- Backend modules: `apps/api/Modules/Auth`, `Account`, `Catalog`, `Requests`.
- Infrastructure: database, hosting, reverse proxy/logging/storage under `apps/api/Infrastructure`.
- Explicit SQL repositories and queries under `apps/api/Modules/*/Repositories` and `apps/api/Modules/Catalog/Queries`.
- SQL migrations under `apps/dbmigrator/Migrations`.
- Frontend routes under `apps/front/src/app`, UI components under `apps/front/src/components`, API clients under `apps/front/src/lib/api`.
- Import tooling under `apps/catalog-import.core` and `apps/catalog-import.winforms`.

## Recommended Build Order

1. Stabilize auth and production configuration because this has low product-dependency risk and high release value.
2. Harden API error handling and frontend transport normalization because it improves every admin/customer workflow.
3. Define storage boundary and diagnostics before adding web import/export or non-public file workflows.
4. Stabilize SEO/GEO sitemap/canonical behavior before adding landing pages or more public catalog routes.
5. Decompose large admin UI containers before extending admin workflows.
6. Add contract drift checks before broadening frontend/backend DTO surfaces.

## Integration Boundaries

- Database and filesystem are not one atomic resource; storage plans need staging, compensation and diagnostics.
- Public SEO routes depend on backend public catalog API availability and production origin configuration.
- Admin frontend depends on CSRF/session behavior and handwritten TypeScript DTOs.
- DbUp migrations are a hard boundary for any schema change.

## Architectural Pitfall To Avoid

Do not treat release hardening as a set of isolated fixes. Auth, storage, SEO/GEO and production configuration should each receive explicit verification criteria and tests, otherwise the project will keep hidden operational debt.
