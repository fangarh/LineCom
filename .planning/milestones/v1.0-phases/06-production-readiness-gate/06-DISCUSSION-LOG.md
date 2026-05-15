# Phase 6: Production Readiness Gate - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-05-15
**Phase:** 06-production-readiness-gate
**Areas discussed:** Dependency audit policy, Production deployment and Local FileStorage backup/restore documentation, Final verification and release blocker policy

---

## Dependency Audit Policy

| Question | Options Presented | User's Choice |
| --- | --- | --- |
| How should Phase 6 treat dependency audit findings? | Block on critical/high; Block on any vulnerability; Advisory only | Block on critical/high |
| What if advisory data is unavailable because of network or registry issues? | Retry with network, then document if blocked; Always block if unavailable; Use local-only fallback | Retry with network, then document if blocked |
| Which audit commands are mandatory for Phase 6? | npm + NuGet; Only package-manager audits; Audit + outdated | npm + NuGet |
| How should critical/high findings be handled when an upgrade is risky? | Fix if bounded, otherwise explicit waiver; Always upgrade; Always waiver | Fix if bounded, otherwise explicit waiver |

**Notes:** `critical` and `high` findings block unless fixed or explicitly waived. Network/advisory failures must be retried with network access before being documented as blocked evidence.

---

## Production Deployment And Local FileStorage Backup/Restore Documentation

| Question | Options Presented | User's Choice |
| --- | --- | --- |
| How detailed should production docs be in Phase 6? | Runbook with commands and checklist; High-level handoff; Operational automation | Runbook with commands and checklist |
| Should Local FileStorage and PostgreSQL backup/restore be coordinated or separate? | Coordinated DB + storage snapshot; Separate procedures; Storage only | Coordinated DB + storage snapshot |
| Which restore scenario is mandatory in release docs? | Documented dry-run restore; Production rollback only; Manual verification note | Documented dry-run restore |
| Where should the production runbook live? | In vault + planning summary; Only .planning; Separate repo docs | In vault + planning summary |

**Notes:** The production runbook should be updated in `vault/Человекочитаемое/Production deployment line-com.ru.md` and referenced from GSD artifacts.

---

## Final Verification And Release Blocker Policy

| Question | Options Presented | User's Choice |
| --- | --- | --- |
| What final test gate scope is mandatory for Phase 6? | Full backend + frontend focused release suite; Only phase-specific checks; Build-only smoke | Full backend + frontend focused release suite |
| How should Phase 6 handle known unrelated dirty public pages/resolver/errors files? | Exclude from release gate commits; Audit but don't fix; Resolve before closure | Exclude from release gate commits |
| What blocks milestone closure besides failed tests and critical/high dependency findings? | Security/storage/migration/docs gaps block; Only executable failures; Human override allowed broadly | Security/storage/migration/docs gaps block |
| How should the final milestone result be captured? | Verification + milestone summary; Only 06-VERIFICATION.md; PR-ready release notes | Verification + milestone summary |

**Notes:** Known unrelated dirty files remain user-owned baseline. Final closure requires both Phase 6 verification and a milestone summary/closure artifact.

---

## the agent's Discretion

- Exact audit command spelling and project selection.
- Exact runbook section structure.
- Exact milestone closure artifact filename.

## Deferred Ideas

None.
