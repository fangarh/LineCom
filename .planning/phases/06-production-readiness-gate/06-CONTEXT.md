# Phase 6: Production Readiness Gate - Context

**Gathered:** 2026-05-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 6 is the final release-readiness gate for the v1 stabilization milestone. It must verify dependency security posture, production deployment documentation, coordinated PostgreSQL plus Local FileStorage backup/restore expectations, v1 requirement traceability and final release evidence before product expansion resumes.

This phase does not add new product capabilities, SEO landing pages, product comparison, web import/export, generated contract infrastructure, storage-provider replacement, or broad feature refactors. It is a release gate: audit, document, verify and close blockers.

</domain>

<decisions>
## Implementation Decisions

### Dependency audit policy
- **D-01:** Dependency audit findings with `critical` or `high` severity block Phase 6 unless fixed or explicitly waived.
- **D-02:** `moderate` and `low` findings should be documented in the Phase 6 verification notes or backlog unless a direct runtime/security release risk is identified.
- **D-03:** If advisory data is unavailable because of network or registry failure, retry with network access. If the registry is still unavailable, Phase 6 may continue only with blocked-audit evidence and a follow-up recorded.
- **D-04:** Mandatory audit commands are npm audit for the frontend workspace/repo-equivalent and NuGet vulnerable audit with transitive dependency coverage.
- **D-05:** For `critical`/`high` findings, fix bounded and testable upgrades in Phase 6. If remediation requires a risky major upgrade or broad refactor, create an explicit waiver that records impact, exploitability and follow-up.

### Production deployment and backup/restore runbook
- **D-06:** Phase 6 production documentation should be a runbook with commands and checklist coverage for API, frontend, DbUp migrator, PostgreSQL, Local FileStorage, environment variables, backup/restore and release verification.
- **D-07:** Deployment automation is not required for Phase 6 unless planning finds a narrow, low-risk helper script already implied by the runbook.
- **D-08:** Backup/restore must be documented as a coordinated PostgreSQL dump plus Local FileStorage archive for the same backup point.
- **D-09:** Restore guidance must restore both database and storage layers consistently; separate one-layer restore should be treated as a risk scenario, not the default release posture.
- **D-10:** The mandatory restore scenario is a dry-run restore to a separate host/database/storage path, followed by API/frontend verification without touching production.
- **D-11:** The main production runbook should live in `vault/Человекочитаемое/Production deployment line-com.ru.md`; GSD summaries and verification should reference it rather than duplicating the full operational doc.

### Final verification and release blocker policy
- **D-12:** The final Phase 6 gate must include a full backend test run, frontend tests, frontend build and relevant phase-specific verification commands from Phases 1-5.
- **D-13:** Known unrelated dirty public page/style, public homepage resolver and `errors/` changes remain user-owned baseline. Phase 6 must not edit, stage or commit them unless the user explicitly changes scope.
- **D-14:** Phase 6 verification notes must explicitly identify unrelated dirty baseline files if they still exist at closure.
- **D-15:** Milestone closure is blocked by failed tests/builds, unremediated or unwaived `critical`/`high` dependency findings, unresolved security gaps, missing storage/backup/restore documentation, migration/schema drift, production environment ambiguity or missing v1 requirement traceability.
- **D-16:** Human override is allowed only through an explicit verification note/waiver with risk, rationale, owner and follow-up; it is not a default path for closing executable failures.
- **D-17:** Final milestone result should include both `06-VERIFICATION.md` and a separate milestone summary/closure artifact with requirement traceability, verification commands, residual risks and next-phase backlog.

### the agent's Discretion
- Exact audit command spelling and project selection, provided npm and NuGet vulnerable audits are covered and command output is recorded.
- Exact runbook section structure, provided it covers API, frontend, migrator, PostgreSQL, Local FileStorage, environment variables, backup/restore and dry-run restore verification.
- Exact milestone closure artifact filename, provided it is clearly discoverable under `.planning/` and referenced from Phase 6 verification.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and phase scope
- `.planning/PROJECT.md` - project constraints, Local FileStorage target, no intentional technical debt, Dapper/DbUp constraints and dirty-worktree rules.
- `.planning/REQUIREMENTS.md` - Phase 6 requirements `SEC-03`, `PROD-02`, `STOR-05`, `VER-01`, `VER-02` and v1 traceability table.
- `.planning/ROADMAP.md` - Phase 6 goal, success criteria and planned split `06-01`/`06-02`.
- `.planning/STATE.md` - current GSD workflow state and prior phase completion.

### Prior phase verification evidence
- `.planning/phases/01-release-safety-baseline/01-VERIFICATION.md` - auth throttling, production origin/config and API transport verification evidence.
- `.planning/phases/02-storage-access-and-diagnostics/02-VERIFICATION.md` - Local FileStorage access boundary and diagnostics verification.
- `.planning/phases/03-import-storage-consistency/03-VERIFICATION.md` - import staging/promotion/reset storage verification.
- `.planning/phases/04-public-seo-geo-reliability/04-VERIFICATION.md` - public SEO/GEO route, robots, sitemap and build verification.
- `.planning/phases/05-admin-maintainability-and-contracts/05-VERIFICATION.md` - admin maintainability and contract drift verification.

### Codebase map
- `.planning/codebase/ARCHITECTURE.md` - deployment components, API/frontend/migrator/storage architecture and constraints.
- `.planning/codebase/CONCERNS.md` - dependency audit risk, Local FileStorage backup/restore scaling limit, remaining production concerns.
- `.planning/codebase/INTEGRATIONS.md` - production hosting, nginx/TLS, PostgreSQL, Local FileStorage, environment variables and missing CI pipeline.
- `.planning/codebase/TESTING.md` - backend/frontend test commands and verification patterns.
- `.planning/codebase/CONVENTIONS.md` - documentation, testing and implementation conventions.

### Source-of-truth docs
- `vault/Человекочитаемое/Production deployment line-com.ru.md` - primary production runbook to update in Phase 6.
- `vault/Человекочитаемое/Сквозные требования.md` - cross-cutting release, no intentional technical debt and production-readiness requirements.
- `vault/Человекочитаемое/Архитектура backend и БД.md` - backend, DbUp, PostgreSQL and Local FileStorage architecture constraints.
- `vault/Человекочитаемое/Технический стек.md` - runtime stack and deployment component expectations.

### Likely implementation surfaces
- `apps/front/package.json` - npm audit/build/test scripts and frontend dependency surface.
- `package-lock.json` - npm lockfile for audit evidence.
- `LineCom.sln` - solution-level backend test/build surface.
- `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj` - backend test project used for release verification.
- `apps/api/LineCom.Api.csproj` - API dependency surface for NuGet audit.
- `apps/dbmigrator/LineCom.DbMigrator.csproj` - DbUp migrator dependency surface.
- `apps/front/next.config.ts` - frontend production standalone output and API/storage rewrites.
- `apps/api/Program.cs` - API startup and production configuration path.
- `apps/api/Infrastructure/Hosting/ProductionConfigurationGuard.cs` - production environment guardrails.
- `apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs` - Local FileStorage root configuration.
- `apps/dbmigrator/Program.cs` - migration execution entry point.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Phase verification files already contain command evidence and residual risks; Phase 6 can aggregate them instead of rediscovering every result.
- `vault/Человекочитаемое/Production deployment line-com.ru.md` already documents production hosting, nginx/TLS, systemd and release directories, making it the right place for runbook expansion.
- Existing tests already cover release-critical surfaces from Phases 1-5; Phase 6 should run them as a final gate and record failures precisely.
- `gsd-sdk.cmd query verify.schema-drift` is available and was used in earlier phases as a schema-drift gate.

### Established Patterns
- Planning and verification artifacts live under `.planning/phases/{NN}-{slug}` with `{NN}-CONTEXT.md`, `{NN}-PLAN.md`, `{NN}-SUMMARY.md` and `{NN}-VERIFICATION.md`.
- Project documentation source of truth lives in `vault/Человекочитаемое`; Phase 6 should update the operational runbook there and reference it from `.planning`.
- Backend uses .NET 8, Npgsql, Dapper and DbUp SQL migrations; no EF or implicit startup migrations.
- Frontend uses Next.js standalone output and npm scripts; production public origin/API origin guardrails already exist.
- Existing dirty worktree changes outside Phase 6 scope must remain unstaged and explicitly noted when verifying the release gate.

### Integration Points
- Dependency audit connects to npm workspace/lockfile and .NET project/solution packages.
- Production docs connect API, frontend, DbUp migrator, PostgreSQL and Local FileStorage restore procedure.
- Final verification connects all prior phase verification files, full backend/frontend test/build commands, schema-drift check and milestone closure summary.

</code_context>

<specifics>
## Specific Ideas

- Treat Phase 6 as a release gate rather than a feature phase.
- Prefer high-signal blocker policy over broad cleanup: close only release blockers, document non-blocking residual risk.
- A failed advisory registry should not silently pass; it must be retried with network access and then documented if still blocked.
- Restore documentation must be dry-run oriented so production is not used as the first restore test.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope.

</deferred>

---

*Phase: 06-Production Readiness Gate*
*Context gathered: 2026-05-15*
