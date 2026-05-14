---
phase: 03
slug: import-storage-consistency
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-05-14
---

# Phase 03 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 on `net8.0` |
| **Config file** | `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj` |
| **Quick run command** | `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"` |
| **Full suite command** | `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --no-build` |
| **Estimated runtime** | ~30-90 seconds focused, ~2-5 minutes full no-build |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"`
- **After every plan wave:** Run `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --no-build`
- **Before `$gsd-verify-work`:** Full backend no-build suite must be green.
- **Max feedback latency:** 5 minutes.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | STOR-04 | T-03-01 | Staging paths remain private and root-contained. | unit/source | `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"` | yes | pending |
| 03-01-02 | 01 | 1 | STOR-04 | T-03-02 | Failed apply cleanup is scoped to current run only. | unit/source | `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"` | yes | pending |
| 03-01-03 | 01 | 1 | STOR-04 | T-03-03 | Reset physical deletion is scoped to DB-selected import-managed files. | unit/source | `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"` | yes | pending |
| 03-02-01 | 02 | 2 | STOR-04 | T-03-01/T-03-03 | Regression tests prove no unmanaged public files after failure/reset paths. | unit/source/db-optional | `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"` | yes | pending |

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

All Phase 3 behaviors have automated verification through catalog import tests and source/SQL contract checks.

---

## Validation Sign-Off

- [x] All tasks have automated verify commands.
- [x] Sampling continuity: no 3 consecutive tasks without automated verify.
- [x] Wave 0 covers all missing references.
- [x] No watch-mode flags.
- [x] Feedback latency target is less than 5 minutes for focused tests.
- [x] `nyquist_compliant: true` set in frontmatter.

**Approval:** pending
