# Phase 7: Modal Catalog Editors - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-05-15
**Phase:** 7-Modal Catalog Editors
**Areas discussed:** save/delete behavior, modal closing rules, category position controls, modal size and scrolling

---

## Save/Delete Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Recommended | `save/create` keep the modal open with success message; `delete` closes the modal and returns to the list. | Yes |
| Close after any success | Close after successful save, create and delete. | |
| Keep open after delete | Keep the modal open even after deleting the entity. | |
| Custom | User defines another behavior. | |

**User's choice:** "твой вариант"
**Notes:** The accepted recommendation preserves the current editor continuity after save while avoiding a stale deleted-object editor after delete.

---

## Modal Closing Rules

| Option | Description | Selected |
|--------|-------------|----------|
| Recommended | `X`, Escape and backdrop close when no mutation is running; unsaved changes require confirmation. | Yes |
| No guard | Close immediately without unsaved-change confirmation. | |
| Explicit controls only | Only `X` or Cancel close; Escape/backdrop do not close. | |
| Custom | User defines another rule. | |

**User's choice:** "твой вариант"
**Notes:** Close actions must not interrupt in-flight mutations.

---

## Category Position Controls

| Option | Description | Selected |
|--------|-------------|----------|
| Recommended | Keep category form, delete, move and sort in one modal; present move/sort as a compact `Position` section. | Yes |
| Separate modal | Put move/sort into a separate modal. | |
| Keep below list | Leave move/sort outside the modal below the category list. | |
| Custom | User defines another rule. | |

**User's choice:** "твой вариант"
**Notes:** This preserves current category capabilities while removing the always-visible side editor.

---

## Modal Size and Scrolling

| Option | Description | Selected |
|--------|-------------|----------|
| Recommended | Shared wide responsive modal with sticky header/footer and scrollable body; near-fullscreen on narrow viewports. | Yes |
| Compact modal | Smaller dialog, accepting tighter product editor layout. | |
| Always fullscreen | Fullscreen modal on all viewports. | |
| Custom | User defines another layout. | |

**User's choice:** "твой вариант"
**Notes:** Product editor tabs and long category forms need enough working width and stable action placement.

## the agent's Discretion

- Exact CSS class names, spacing and small layout details may follow the current admin catalog styling conventions.
- Managers should continue to own data loading, mutation state and stale-request guards unless implementation research finds a safer local pattern.

## Deferred Ideas

- Quick product category reassignment belongs to Phase 8.
- Bulk category changes remain deferred future scope.
- Broad admin redesign, generated OpenAPI/contracts, product comparison, SEO landing pages and web import/export remain out of scope.
