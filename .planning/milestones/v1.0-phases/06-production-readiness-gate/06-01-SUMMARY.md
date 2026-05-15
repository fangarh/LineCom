---
phase: 06-production-readiness-gate
plan: "01"
subsystem: infra
tags: [security, dependencies, npm, nuget, deployment, backup, restore, local-filestorage]
requires:
  - phase: 05-admin-maintainability-and-contracts
    provides: "Final admin maintainability and contract drift verification before release gate"
provides:
  - "Release dependency audit evidence for npm and NuGet"
  - "Bounded fixes for high dependency audit findings"
  - "Production runbook covering API, frontend, DbUp migrator, PostgreSQL and Local FileStorage"
  - "Coordinated PostgreSQL plus Local FileStorage backup and dry-run restore checklist"
affects: [phase-06-final-verification, production-readiness, release-operations]
tech-stack:
  added:
    - "next 16.2.6"
    - "System.Net.Http 4.3.4 test dependency override"
    - "System.Text.RegularExpressions 4.3.1 test dependency override"
  patterns:
    - "Critical/high dependency audit findings are fixed or explicitly waived before release closure"
    - "Local FileStorage restore is validated together with PostgreSQL restore"
key-files:
  created:
    - ".planning/phases/06-production-readiness-gate/06-01-AUDIT.md"
  modified:
    - "apps/front/package.json"
    - "apps/front/package-lock.json"
    - "tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj"
    - "vault/Человекочитаемое/Production deployment line-com.ru.md"
key-decisions:
  - "Fixed all critical/high audit findings instead of creating waivers."
  - "Kept NuGet remediation limited to the test project because production projects audited clean."
  - "Documented restore as a coordinated database plus Local FileStorage operation, not separate one-layer recovery."
patterns-established:
  - "Audit evidence records command, exit status, network retry behavior, findings, fix and post-fix verification."
  - "Production runbook stores variable names and paths only, not secret values."
requirements-completed: [SEC-03, PROD-02, STOR-05]
duration: 27min
completed: 2026-05-15
---

# Phase 6 Plan 01: Dependency Audit And Production Recovery Summary

**Dependency security gate with bounded npm/NuGet fixes plus production backup/restore runbook for PostgreSQL and Local FileStorage**

## Performance

- **Duration:** 27 min
- **Started:** 2026-05-15T10:23:00+03:00
- **Completed:** 2026-05-15T10:50:21+03:00
- **Tasks:** 5
- **Files modified:** 5

## Accomplishments

- Ran npm audit, found one high `next` vulnerability record, upgraded `next` from `16.2.4` to `16.2.6`, and verified npm audit returned `0` vulnerabilities.
- Ran NuGet vulnerable audit with transitive coverage, found two high transitive test-project findings, pinned patched direct test dependencies, and verified the solution reported no vulnerable packages.
- Expanded the production runbook with release commands, production config checklist, DbUp migration procedure, coordinated backup point, dry-run restore and post-restore smoke checks.
- Preserved the known unrelated dirty public page/style, homepage resolver and `errors/` baseline as user-owned work.

## Task Commits

1. **Tasks 1-3: dependency audit boundary, npm audit and NuGet audit** - `51cb2b5` (`chore(06-01)`)
2. **Task 4: production deployment and restore runbook** - `758db53` (`docs(06-01)`)
3. **Task 5: summary and GSD metadata** - recorded by the plan metadata commit.

## Files Created/Modified

- `.planning/phases/06-production-readiness-gate/06-01-AUDIT.md` - command evidence, findings, fixes, waivers and post-fix verification.
- `apps/front/package.json` - bumped `next` to `16.2.6`.
- `apps/front/package-lock.json` - locked Next.js and SWC packages at `16.2.6`.
- `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj` - pinned patched `System.Net.Http` and `System.Text.RegularExpressions` test dependency versions.
- `vault/Человекочитаемое/Production deployment line-com.ru.md` - added release runbook and coordinated backup/restore procedure.

## Decisions Made

- Fixed all high findings instead of waiving them because both remediations were bounded and testable.
- Used `dotnet list LineCom.sln package --vulnerable --include-transitive` as the authoritative NuGet audit command because the installed SDK does not support `dotnet package list`.
- Treated intermittent `NU1900` restore advisory-fetch warnings as a recorded operational concern, not as a clean audit result.

## Deviations from Plan

None - plan executed exactly as written. The dependency updates were explicitly allowed by the plan for bounded critical/high fixes.

## Issues Encountered

- Initial npm audit failed under sandbox network restrictions; it was retried with approved network access and then produced actionable findings.
- `dotnet restore LineCom.sln` intermittently emitted `NU1900` warnings for vulnerability data fetch even after network retry. The dedicated vulnerable audit command reached NuGet sources and completed cleanly after fixes.
- Optional current .NET CLI syntax `dotnet package list ... --format json` is not available in the installed SDK; fallback command is documented in `06-01-AUDIT.md`.

## Verification

- `npm.cmd --prefix apps/front audit --json` - passed, `0` vulnerabilities after `next@16.2.6`.
- `dotnet list LineCom.sln package --vulnerable --include-transitive` - passed, no vulnerable packages after test dependency pins.
- `npm.cmd --prefix apps/front test` - passed, `68` test files and `294` tests.
- `dotnet test LineCom.sln` - passed, `770` tests.
- `$env:LINECOM_PUBLIC_SITE_ORIGIN='https://line-com.ru'; npm.cmd --prefix apps/front run build` - passed on Next.js `16.2.6`.
- `git diff --check` for audit/runbook files - passed.
- `Select-String` checks confirmed the runbook covers API, frontend, dbmigrator, PostgreSQL, Local FileStorage, backup, restore and dry-run restore.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Plan `06-02` can proceed to final release verification and milestone closure. It should reference `06-01-AUDIT.md` and the production runbook as the evidence for `SEC-03`, `PROD-02` and `STOR-05`.

Known unrelated dirty baseline remains present and unstaged:

- public about/delivery/homepage files;
- public style files;
- homepage curated product resolver files;
- `errors/`.

---
*Phase: 06-production-readiness-gate*
*Completed: 2026-05-15*
