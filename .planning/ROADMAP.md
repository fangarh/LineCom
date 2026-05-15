# Roadmap: LineCom Release Stabilization

## Overview

The first GSD milestone stabilizes the existing LineCom release before adding new product scope. The roadmap starts with auth, production configuration and API error behavior, then hardens Local FileStorage, catalog import DB/file consistency, SEO/GEO routes, admin maintainability and final production readiness.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work.
- Decimal phases (2.1, 2.2): Urgent insertions marked as INSERTED.

- [x] **Phase 1: Release Safety Baseline** - harden auth, production origin checks and frontend API error normalization. Completed: 2026-05-14.
- [x] **Phase 2: Storage Access And Diagnostics** - define public/private storage boundaries and integrity reporting. Completed: 2026-05-14.
- [x] **Phase 3: Import Storage Consistency** - make catalog import DB/file behavior recoverable and testable. Completed: 2026-05-14.
- [x] **Phase 4: Public SEO/GEO Reliability** - verify canonical, robots, sitemap and scalable public route behavior. Completed: 2026-05-14.
- [ ] **Phase 5: Admin Maintainability And Contracts** - reduce frontend admin fragility and detect API contract drift.
- [ ] **Phase 6: Production Readiness Gate** - run final release checks, audits, docs and GSD verification.

## Phase Details

### Phase 1: Release Safety Baseline
**Goal**: Login/register abuse protection, production origin validation and controlled frontend API errors are in place.
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: SEC-01, SEC-02, PROD-01, PROD-03, API-01, API-02
**Success Criteria** (what must be TRUE):
  1. Repeated login/register attempts hit a tested throttling policy and return controlled 429 behavior.
  2. Production configuration cannot silently generate localhost canonical, robots or sitemap URLs.
  3. Frontend API clients surface non-JSON and malformed upstream errors as controlled client errors.
  4. Auth cookie settings are verified against production HTTPS requirements.
**Plans**: 3 plans

Plans:
- [x] 01-01: Auth rate limiting and cookie production verification.
- [x] 01-02: Production origin and environment guardrails.
- [x] 01-03: Frontend API transport error normalization and tests.

### Phase 2: Storage Access And Diagnostics
**Goal**: Local FileStorage has explicit public/private boundaries and diagnostics for DB/disk drift.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: STOR-01, STOR-02, STOR-03
**Success Criteria** (what must be TRUE):
  1. Anonymous `/storage` access cannot reach non-public file purposes.
  2. Public catalog images remain available through the supported public path.
  3. A diagnostic path reports missing files, untracked files and stale stored file rows.
  4. Storage boundary and diagnostics are covered by tests.
**Plans**: 3 plans

Plans:
- [x] 02-01: Public/private storage serving policy.
- [x] 02-02: Storage integrity diagnostic model and report.
- [x] 02-03: Storage boundary and diagnostic tests.

### Phase 3: Import Storage Consistency
**Goal**: Catalog import no longer leaves unmanaged DB/file inconsistencies after apply/reset failures.
**Mode:** mvp
**Depends on**: Phase 2
**Requirements**: STOR-04
**Success Criteria** (what must be TRUE):
  1. Import image writes follow a documented staging, promotion and cleanup flow.
  2. Failed import apply paths do not leave unmanaged public files without DB rows.
  3. Reset behavior has explicit physical-file cleanup or documented retention behavior.
  4. Import consistency tests cover DB failure and reset edge cases.
**Plans**: 2 plans

Plans:
- [x] 03-01: Import file staging/promotion/cleanup behavior.
- [x] 03-02: Import consistency and reset regression tests.

### Phase 4: Public SEO/GEO Reliability
**Goal**: Public catalog SEO/GEO output is production-safe, test-covered and scalable enough for catalog growth.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: SEO-01, SEO-02, SEO-03
**Success Criteria** (what must be TRUE):
  1. Representative catalog and product routes generate expected canonical metadata from validated origin.
  2. `robots.ts` and `sitemap.ts` output production-safe URLs and sitemap references.
  3. Sitemap generation has segmented, cached or bounded behavior documented and tested.
  4. SEO/GEO requirements remain visible in route/API change verification.
**Plans**: 3 plans

Plans:
- [x] 04-01: Canonical metadata and route verification.
- [x] 04-02: Sitemap scaling strategy.
- [x] 04-03: Robots/sitemap/metadata regression tests.

### Phase 5: Admin Maintainability And Contracts
**Goal**: Admin catalog/homepage code is safer to extend and critical DTO drift is detectable.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: MAIN-01, MAIN-02, MAIN-03
**Success Criteria** (what must be TRUE):
  1. Large admin containers touched by release work are decomposed before behavior is added.
  2. Mapping and payload-building helpers have focused unit tests.
  3. Critical frontend API client shapes are checked against backend DTO/endpoint expectations.
  4. Existing admin catalog/homepage tests continue to pass.
**Plans**: 3 plans

Plans:
- [x] 05-01: Admin catalog/homepage decomposition targets.
- [x] 05-02: Helper extraction and frontend unit tests.
- [x] 05-03: Frontend/backend API contract drift checks.

### Phase 6: Production Readiness Gate
**Goal**: The stabilized release is documented, audited and verified through GSD before product expansion resumes.
**Mode:** mvp
**Depends on**: Phases 1-5
**Requirements**: SEC-03, PROD-02, STOR-05, VER-01, VER-02
**Success Criteria** (what must be TRUE):
  1. Dependency audit is run or explicitly recorded as blocked by network/tooling with follow-up.
  2. Production deployment docs cover API, frontend, migrator, PostgreSQL and Local FileStorage backup/restore.
  3. All v1 requirements have traceability and verification evidence.
  4. GSD verifier confirms no intentional technical debt, security gap or migration risk remains for this milestone.
**Plans**: 2 plans

Plans:
- [ ] 06-01: Dependency audit and deployment/storage documentation.
- [ ] 06-02: Final release verification and milestone closure.

## Deferred Product Phases

These are intentionally outside the release-stabilization milestone and should be revisited after Phase 6:

- Product comparison by normalized attributes.
- SEO/GEO landing pages.
- Web-based import/export workflow.

## Progress

**Execution Order:**
Phases execute in dependency order: 1 -> 2 -> 3 -> 4 -> 5 -> 6. Phases 4 and 5 can be planned in parallel after Phase 1 if dependencies remain disjoint.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Release Safety Baseline | 3/3 | Complete | 2026-05-14 |
| 2. Storage Access And Diagnostics | 3/3 | Complete | 2026-05-14 |
| 3. Import Storage Consistency | 2/2 | Complete | 2026-05-14 |
| 4. Public SEO/GEO Reliability | 3/3 | Complete | 2026-05-14 |
| 5. Admin Maintainability And Contracts | 0/3 | Not started | - |
| 6. Production Readiness Gate | 0/2 | Not started | - |
