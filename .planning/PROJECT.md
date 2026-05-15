# LineCom

## What This Is

LineCom - B2B/B2C каталог-заявочная система для продажи кабеля, СКС/ВОЛС-компонентов и сопутствующих товаров. Система не является классическим интернет-магазином с публичными ценами и онлайн-оплатой: покупатель собирает товары в заявку, а продавец обрабатывает ее вручную вне публичного checkout.

Проект уже является существующим codebase: ASP.NET Core API, PostgreSQL через Npgsql/Dapper, DbUp SQL-миграции, Next.js frontend, WinForms/core контур импорта каталога и локальное файловое хранилище.

## Core Value

Покупатель должен находить нужные кабельные товары через SEO/GEO-доступный каталог и надежно отправлять коммерческую заявку, которую продавец может обработать без потери данных.

## Current State

v1.0 Release Stabilization shipped on 2026-05-15. The milestone is archived in `.planning/milestones/` and the raw phase execution history is archived in `.planning/milestones/v1.0-phases/`.

The release baseline is verified for auth throttling, production configuration guardrails, frontend API transport errors, Local FileStorage public/private boundaries, storage diagnostics, catalog import file lifecycle, SEO/GEO route behavior, admin maintainability/contract drift checks, dependency audits, production runbook coverage and final requirement traceability.

Milestone v1.1 Admin Catalog UX is in planning. It focuses on the existing admin catalog product/category editing workflow: modal editors replace the always-visible side editors, and product rows gain a focused quick category change action.

## Current Milestone: v1.1 Admin Catalog UX

**Goal:** Improve catalog admin usability by moving product/category editing into modal dialogs and adding a safe single-product quick category change flow.

**Target features:**
- Product editor opens in a modal from product row selection and `New product`.
- Category editor opens in a modal from category selection and `New category`.
- Product rows expose a quick category change action that preserves current product fields.
- Quick category change warns before saving when category-specific attribute values may be cleared or invalidated.
- Focused frontend tests and desktop/narrow viewport QA cover the new editing flows.

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
- Phase 4 validated public SEO/GEO reliability for canonical metadata, robots, bounded sitemap generation and focused route/helper regression tests: `.planning/phases/04-public-seo-geo-reliability/04-VERIFICATION.md`.
- Phase 5 validated admin maintainability and lightweight frontend/backend contract drift checks for current dirty admin catalog/homepage areas: `.planning/phases/05-admin-maintainability-and-contracts/05-VERIFICATION.md`.
- Phase 6 validated dependency audits, production deployment documentation, coordinated PostgreSQL plus Local FileStorage backup/restore guidance, final release checks and v1 traceability: `.planning/phases/06-production-readiness-gate/06-VERIFICATION.md`.

### Active

- Modal product editing for the existing admin catalog product manager.
- Modal category editing for the existing admin catalog category manager.
- Safe single-product quick category change from the product list.
- Focused regression coverage for modal editing, quick category changes, stale request handling and responsive behavior.

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
- Phase 6 documents coordinated PostgreSQL plus Local FileStorage backup/restore and dry-run restore checks; broader retention/cleanup automation remains future work.
- Auth login/register endpoint throttling is validated by Phase 1.
- Production SEO origin is validated by Phase 1 guardrails and Phase 4 build/test evidence.
- Sitemap generation is bounded by Phase 4 release limits; segmented sitemap generation remains the future growth path when catalog size exceeds those limits.
- Phase 5 bounded current dirty admin catalog/homepage decomposition scope and added focused helper/contract checks.
- Frontend API contracts are still handwritten, but Phase 5 added lightweight critical admin API drift tests for v1.

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
| Roadmap focus: release stabilization first | Security, storage, SEO/GEO and production readiness risks block safe expansion of product scope. | Good - v1 release-stabilization work verified through Phase 6 on 2026-05-15 |
| Granularity: Standard | 5-8 phases gives useful control without excessive planning overhead. | Good - v1 closed as 6 phases and 16 plans |
| Execution: Parallel where safe | Independent plans can run in parallel, while migrations/security/storage remain dependency-driven. | Good - bounded parallelism worked where dependencies were disjoint |
| Research: Full, but source-of-truth constrained | External/current docs inform best practices; `vault` and codebase map remain authoritative for project intent. | Good - phase research informed implementation without overriding project rules |
| Plan Check and Verifier enabled | Large release-critical work needs pre-execution plan validation and post-execution goal verification. | Good - every phase has verification evidence |
| Commit planning docs to git | GSD state should persist across sessions, but commits must avoid unrelated product changes. | Good - v1 planning artifacts were committed without staging user-owned dirty baseline |
| v1.1 scope: modal admin catalog UX first | User identified side editor blocks as the immediate usability pain; quick category changes are useful, while bulk category changes would add extra state and risk. | Pending |

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
*Last updated: 2026-05-15 for v1.1 milestone start*
