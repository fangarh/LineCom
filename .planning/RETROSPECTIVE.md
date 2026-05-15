# Retrospective

## Milestone: v1.0 - Release Stabilization

**Shipped:** 2026-05-15
**Phases:** 6
**Plans:** 16
**Tasks:** 55

### What Was Built

v1.0 stabilized the existing LineCom release before product expansion. The milestone added or verified auth throttling, production origin/configuration guardrails, controlled frontend API transport errors, Local FileStorage public/private boundaries, read-only storage diagnostics, catalog import staging/promotion/reset behavior, public SEO/GEO route reliability, admin helper decomposition, lightweight admin API contract drift checks, dependency audit closure, production deployment documentation and final GSD traceability.

### What Worked

- Keeping `vault/Человекочитаемое` and `.planning/codebase/` as authority prevented scope expansion during release hardening.
- Narrow phase goals made it possible to verify security, storage, SEO/GEO, maintainability and production readiness independently.
- Focused helper and contract tests gave useful coverage without introducing generated OpenAPI infrastructure.
- Explicit dirty-worktree handling kept user-owned public page/style changes out of planning and product commits.

### What Was Inefficient

- Missing global GSD subagents forced inline fallback for agent-oriented workflows.
- NuGet advisory-feed availability caused repeated `NU1900` noise during test/restore commands.
- Nyquist validation artifacts were not consistently generated for every phase even though verification artifacts were complete.

### Patterns Established

- Treat Local FileStorage as the target storage approach and harden boundaries, diagnostics and backup/restore around it.
- Prefer lightweight contract drift tests for critical API surfaces before adopting heavier generated contract tooling.
- Keep admin decomposition scoped to touched dirty areas and extract pure helpers with focused tests.
- Archive completed milestones aggressively so live ROADMAP/REQUIREMENTS stay small.

### Key Lessons

- Final milestone closure should run `$gsd-audit-milestone` before archive to expose process gaps separately from product blockers.
- Dependency audits should stay in the release checklist because restore/test warning behavior is not enough to prove vulnerability status.
- Future milestones should decide whether strict Nyquist `*-VALIDATION.md` coverage is required before execution starts.

## Cross-Milestone Trends

| Trend | Observation |
|-------|-------------|
| Scope control | Release stabilization stayed bounded; deferred product expansion remained outside v1.0. |
| Verification | Automated backend/frontend tests, production build, audit commands and schema-drift checks were sufficient for v1.0 closure. |
| Process debt | Validation artifact coverage needs a clearer rule in the next milestone. |

