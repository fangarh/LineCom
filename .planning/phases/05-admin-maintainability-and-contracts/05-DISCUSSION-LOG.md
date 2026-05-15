# Phase 5: Admin Maintainability And Contracts - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-15
**Phase:** 05-admin-maintainability-and-contracts
**Areas discussed:** Границы декомпозиции, existing dirty changes, contract drift gate, helper extraction priority

---

## Границы декомпозиции

| Option | Description | Selected |
|--------|-------------|----------|
| Только затронутые dirty areas | Фокус на текущих изменениях в `admin-product-*`, `admin-category-*`, `admin-homepage-*` и связанных тестах. Меньше scope, лучше контроль git. | ✓ |
| Все крупнейшие admin containers | Включить `admin-attribute-manager.tsx`, `admin-brand-manager.tsx`, `admin-product-manager.tsx`, `admin-category-manager.tsx`, `admin-homepage-manager.tsx`. Больше пользы, но выше риск затянуть фазу. | |
| Только product + homepage contracts | Сфокусироваться на наиболее контрактно-опасных местах: product admin и homepage admin, category/attribute/brand трогать только если они блокируют. | |

**User's choice:** Только затронутые dirty areas.
**Notes:** Phase 5 не должна превращаться в общий refactor всей админки.

---

## Existing Dirty Changes

| Option | Description | Selected |
|--------|-------------|----------|
| Treat as user-owned baseline | Не откатывать и не включать автоматически; планы должны читать diff, работать поверх него и коммитить только явно относящиеся Phase 5 изменения. | ✓ |
| Fold into Phase 5 if relevant | Считать релевантные dirty changes частью Phase 5 execution и планировать их доведение до тестов/коммитов. | |
| Require pre-cleanup before execution | Сначала вручную/отдельно разделить dirty changes, и только потом выполнять Phase 5. | |

**User's choice:** Treat as user-owned baseline.
**Notes:** Executor must inspect relevant diffs and avoid staging unrelated pre-existing work.

---

## Contract Drift Gate

| Option | Description | Selected |
|--------|-------------|----------|
| Lightweight critical shape tests | Focused тесты сверяют ключевые frontend DTO/API client expectations с backend endpoint JSON shape через backend endpoint tests и frontend API client tests. Без генерации артефакта. | ✓ |
| Backend serialization snapshots + frontend fixtures | Backend endpoint tests фиксируют JSON examples/snapshots, frontend tests используют matching fixtures. Надёжнее, но больше поддержки. | |
| OpenAPI/generated artifact path | Добавить или зафиксировать generated contract artifact и сверять frontend с ним. Самое формальное, но может выйти за release-stabilization scope. | |

**User's choice:** Lightweight critical shape tests.
**Notes:** OpenAPI/generated framework deferred outside Phase 5.

---

## Helper Extraction Priority

| Option | Description | Selected |
|--------|-------------|----------|
| Payload builders and mapping | Команды create/update, DTO→form, form→payload, normalization перед API вызовами. Лучшее покрытие MAIN-02 и contract-risk. | |
| Tree/reorder helpers | Category tree flattening, parent picker eligibility, sort/reorder behavior. Важно для текущих category dirty changes. | |
| Homepage target resolution | Поиск/привязка товаров/категорий для homepage sections. Важно, если homepage dirty changes считаются основной болью. | |
| Balanced by dirty diff | Planner должен выбрать порядок по фактическому diff: сначала helpers, которые уже затронуты и требуют тестов. | ✓ |

**User's choice:** Balanced by dirty diff.
**Notes:** Current dirty areas include product main fields, category parent picker/tree helpers and homepage target search/tests.

---

## the agent's Discretion

- Exact helper/module split boundaries.
- Exact test names and fixture shapes.
- Exact wave ordering, provided dirty-worktree handling is explicit.

## Deferred Ideas

- Generated OpenAPI or frontend/backend DTO generation framework.
- Full admin manager rewrite across all catalog/admin surfaces.
- New admin features, SEO landing pages, product comparison and web import/export.
