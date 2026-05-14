# LineCom

## What This Is

LineCom - B2B/B2C каталог-заявочная система для продажи кабеля, СКС/ВОЛС-компонентов и сопутствующих товаров. Система не является классическим интернет-магазином с публичными ценами и онлайн-оплатой: покупатель собирает товары в заявку, а продавец обрабатывает ее вручную вне публичного checkout.

Проект уже является существующим codebase: ASP.NET Core API, PostgreSQL через Npgsql/Dapper, DbUp SQL-миграции, Next.js frontend, WinForms/core контур импорта каталога и локальное файловое хранилище.

## Core Value

Покупатель должен находить нужные кабельные товары через SEO/GEO-доступный каталог и надежно отправлять коммерческую заявку, которую продавец может обработать без потери данных.

## Requirements

### Validated

- Existing public catalog browsing exists through Next.js App Router pages and public catalog API: `apps/front/src/app/catalog`, `apps/front/src/app/products`, `apps/api/Modules/Catalog`.
- Existing product/category/brand/attribute administration exists in backend and frontend admin surfaces: `apps/api/Modules/Catalog`, `apps/front/src/components/admin/catalog`.
- Existing homepage section administration exists through admin homepage API and frontend manager components: `apps/api/Modules/Catalog`, `apps/front/src/components/admin/homepage`.
- Existing authentication and account/request flow exists with cookie auth, CSRF checks, customer profile, request draft and admin request processing: `apps/api/Modules/Auth`, `apps/api/Modules/Account`, `apps/api/Modules/Requests`, `apps/front/src/components/request`.
- Existing database migration flow uses DbUp SQL scripts in `apps/dbmigrator/Migrations`.
- Existing catalog import tooling exists through `apps/catalog-import.core` and `apps/catalog-import.winforms`.
- Existing Local FileStorage is implemented and intentionally remains the target storage approach: `apps/api/Infrastructure/Storage`, `apps/api/Infrastructure/Hosting`.
- Existing backend and frontend regression coverage exists through xUnit and Vitest: `tests/LineCom.Api.Tests`, `apps/front/src/**/*.test.*`.
- Phase 1 validated release-critical auth throttling, production public origin/configuration guardrails, and frontend API transport error normalization: `.planning/phases/01-release-safety-baseline/01-VERIFICATION.md`.
- Phase 2 validated Local FileStorage public/private static boundaries and read-only DB/disk diagnostics: `.planning/phases/02-storage-access-and-diagnostics/02-VERIFICATION.md`.
- Phase 3 validated catalog import Local FileStorage staging, post-commit promotion, scoped cleanup and reset physical-file regression coverage: `.planning/phases/03-import-storage-consistency/03-VERIFICATION.md`.

### Active

- [ ] Continue Local FileStorage release hardening: backup/restore posture and future maintenance/retention decisions.
- [ ] Preserve SEO/GEO correctness for public catalog routes, metadata, robots and sitemap behavior under production configuration.
- [ ] Reduce fragility of large admin catalog/homepage frontend containers before adding more behavior.
- [ ] Add verification gates for security, storage, SEO/GEO and frontend/backend contract drift.

### Out of Scope

- Online payment and automatic paid-order checkout - product model keeps payment, invoice, shipment and legal order fixation outside the website for the release model.
- Public product prices and exact stock balances - release model uses "Цена по запросу" and coarse availability statuses.
- Entity Framework - backend data access is explicitly Npgsql/Dapper with SQL migrations through DbUp.
- S3/MinIO replacement for storage - Local FileStorage is the project target, not a temporary substitute.
- Immediate implementation of product comparison, SEO landing pages and web import/export - these are tracked as deferred product phases after release stabilization.

## Context

Primary human-readable source of truth is `vault/Человекочитаемое`. It defines product model, backend/database architecture, release data model, SEO/GEO requirements, production deployment and API contracts.

The codebase map created during GSD initialization lives in `.planning/codebase/` and covers stack, integrations, architecture, structure, conventions, testing and concerns.

Important current concerns from the codebase map:

- Phase 2 restricts anonymous static storage access to public product/brand image paths and adds read-only DB/disk diagnostics.
- Phase 3 makes catalog import DB/file behavior recoverable through private staging, post-commit promotion, scoped cleanup and reset physical-file reporting.
- Stored file status supports lifecycle concepts, but backup/restore posture and broader retention policy remain future work.
- Auth endpoints do not yet have rate limiting or account/IP throttling.
- Production SEO origin can silently fall back to localhost if `LINECOM_PUBLIC_SITE_ORIGIN` is missing.
- Sitemap generation currently scales linearly with product count.
- Several admin frontend containers are large stateful components that mix loading, mutation guards, data mapping and rendering.
- Frontend API contracts are handwritten and can drift from backend DTOs without explicit contract checks.

## Constraints

- **Source of truth**: `vault/Человекочитаемое` overrides assumptions from code inspection when product or architecture intent conflicts.
- **Backend stack**: ASP.NET Core/.NET 8, PostgreSQL, Npgsql and Dapper; no Entity Framework.
- **Migrations**: SQL scripts executed by DbUp; schema changes must include migration and database tests where relevant.
- **Storage**: Local FileStorage is the target; release hardening must improve boundaries, diagnostics and backup/restore posture without switching storage class.
- **SEO/GEO**: Any catalog, routing, metadata, sitemap, canonical URL or public content change must preserve SEO/GEO behavior.
- **No intentional technical debt**: Plans must include debt checks, security gaps, migration risks and maintainability review before completion.
- **Frontend decomposition**: Large implementation files should be split before more behavior is added when they mix orchestration, mapping and rendering.
- **Git state**: Product files had pre-existing uncommitted changes during initialization; GSD commits must include only `.planning/` artifacts unless explicitly approved otherwise.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use GSD for this brownfield project | Existing codebase is large enough to benefit from persistent planning, roadmap and verification artifacts. | Pending |
| Start with `$gsd-map-codebase` | Brownfield initialization needs architecture/stack/risk map before project roadmap. | Good |
| Roadmap focus: release stabilization first | Security, storage, SEO/GEO and production readiness risks block safe expansion of product scope. | Good - Phase 1, Phase 2 and Phase 3 release-stabilization work verified 2026-05-14 |
| Granularity: Standard | 5-8 phases gives useful control without excessive planning overhead. | Pending |
| Execution: Parallel where safe | Independent plans can run in parallel, while migrations/security/storage remain dependency-driven. | Pending |
| Research: Full, but source-of-truth constrained | External/current docs inform best practices; `vault` and codebase map remain authoritative for project intent. | Pending |
| Plan Check and Verifier enabled | Large release-critical work needs pre-execution plan validation and post-execution goal verification. | Pending |
| Commit planning docs to git | GSD state should persist across sessions, but commits must avoid unrelated product changes. | Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition**:
1. Requirements invalidated? Move to Out of Scope with reason.
2. Requirements validated? Move to Validated with phase reference.
3. New requirements emerged? Add to Active.
4. Decisions to log? Add to Key Decisions.
5. "What This Is" still accurate? Update if drifted.

**After each milestone**:
1. Full review of all sections.
2. Core Value check - still the right priority?
3. Audit Out of Scope - reasons still valid?
4. Update Context with current state.

---
*Last updated: 2026-05-14 after Phase 3 verification*
