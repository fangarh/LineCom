# Phase 4: Public SEO/GEO Reliability - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-05-14
**Phase:** 04-Public SEO/GEO Reliability
**Areas discussed:** Sitemap scaling strategy, Canonical verification depth, Robots and sitemap references, SEO/GEO regression gate

---

## Sitemap Scaling Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Bounded single sitemap | Keep `/sitemap.xml`, remove unbounded enumeration with explicit release limits and tests. | yes |
| Cached single sitemap | Keep one sitemap and rely on explicit cache/revalidate behavior. | |
| Segmented sitemap via `generateSitemaps` | Split sitemap files for large catalogs. | |
| Agent decides | Let the agent choose the conservative release-stabilization path. | |

**User's choice:** Bounded single sitemap.
**Notes:** The user delegated the exact release boundary to the agent. The agent selected a combined limit: maximum product URLs plus maximum public product API pages.

| Follow-up Option | Description | Selected |
|------------------|-------------|----------|
| Hard URL limit | Cap the number of product URLs. | |
| Hard API-page limit | Cap public product API pages loaded by sitemap. | |
| Combined URL and page limit | Protect against both inflated totals and excessive URL output. | yes |
| Agent decides | Let the agent pick the lowest-risk implementation. | yes |

| Exceed-limit Option | Description | Selected |
|---------------------|-------------|----------|
| Truncate silently | Return a valid bounded sitemap without extra documentation. | |
| Truncate and document as release limit | Return a valid bounded sitemap and document the limit. | yes |
| Fail sitemap | Do not return sitemap when the catalog exceeds the limit. | |
| Agent decides | Let the agent pick. | |

| Limit-location Option | Description | Selected |
|-----------------------|-------------|----------|
| Constants near `app/sitemap.ts` | Keep named limits beside route behavior and tests. | yes |
| Helper in `lib/seo/sitemap.ts` | Centralize in the sitemap helper. | |
| Env/config | Make limits configurable. | |
| Agent decides | Let the agent pick. | |

---

## Canonical Verification Depth

| Option | Description | Selected |
|--------|-------------|----------|
| Route-level metadata tests | Test `generateMetadata` for category/product routes with mocked public API data. | yes |
| Helper/API DTO tests only | Keep verification around helpers and types only. | |
| Browser QA only | Verify representative routes manually in browser. | |
| Route tests plus browser QA note | Require route tests and optionally record browser QA when cheap. | |

**User's choice:** Route-level metadata tests.
**Notes:** Required routes are the category and product happy paths only. API failure/noindex fallback is considered existing sufficient behavior and does not need a new discussion area.

| Route Coverage Option | Description | Selected |
|-----------------------|-------------|----------|
| Category + product happy paths | One representative route-level metadata test per entity type. | yes |
| Happy paths + API unavailable fallback | Add noindex fallback coverage. | |
| Happy paths + 404 behavior | Add `notFound()` coverage. | |
| Full matrix | Cover happy paths, unavailable fallback and 404. | |

| Test Location Option | Description | Selected |
|----------------------|-------------|----------|
| Beside route files | Place tests next to `page.tsx` route files. | yes |
| In `lib/seo` | Centralize all SEO tests under helper directory. | |
| One shared route SEO test file | Use one combined test file for all route metadata. | |
| Agent decides | Let the agent pick. | |

---

## Robots and Sitemap References

| Option | Description | Selected |
|--------|-------------|----------|
| Single `/sitemap.xml` reference | Keep `robots.ts` aligned with bounded single sitemap strategy. | yes |
| Prepare array of sitemap URLs | Refactor toward future segmented sitemap support. | |
| Add future segmented URLs now | Reference sitemap URLs that Phase 4 does not implement. | |
| Agent decides | Let the agent pick. | |

**User's choice:** Keep a single absolute `/sitemap.xml` reference.
**Notes:** Segmented sitemap URLs must not be added before segmented files exist.

| Assertion Option | Description | Selected |
|------------------|-------------|----------|
| Production-safe sitemap + host + disallow list | Assert `sitemap`, `host`, `allow`, and internal-path `disallow`. | yes |
| Only sitemap/host origin | Check only origin-sensitive fields. | |
| Include future route exclusions | Check landing/admin future route behavior. | |
| Agent decides | Let the agent pick. | |

| Code-change Option | Description | Selected |
|--------------------|-------------|----------|
| Tests first | Change production code only if tests reveal a gap. | yes |
| Extract robots helper | Refactor robots construction into a helper. | |
| Do not touch robots | Leave without new evidence. | |
| Agent decides | Let the agent pick. | |

---

## SEO/GEO Regression Gate

| Option | Description | Selected |
|--------|-------------|----------|
| Test regression surface + phase docs | Make tests the gate and document sensitive surfaces in phase artifacts. | yes |
| Separate markdown checklist | Add a human checklist under `.planning`. | |
| Code comments | Add reminders near route/API helpers. | |
| Backend/frontend contract tests | Build broader DTO drift checks. | |

**User's choice:** Test regression surface plus phase docs.
**Notes:** The gate must stay within Phase 4 and avoid expanding into Phase 5 admin/API contract drift.

| Sensitive Surface Option | Description | Selected |
|--------------------------|-------------|----------|
| Public routes + metadata + sitemap/robots + public catalog API SEO fields | Cover public SEO/GEO route and API SEO field changes. | yes |
| Frontend routes/files only | Ignore backend/API SEO field changes. | |
| All catalog API/admin changes | Expand to broad catalog/admin surface. | |
| Agent decides | Let the agent pick. | |

| Verification Option | Description | Selected |
|---------------------|-------------|----------|
| Focused Vitest suite + build | Run focused SEO metadata/sitemap/robots/route tests and frontend production build. | yes |
| Focused Vitest only | Skip frontend build. | |
| Full frontend test suite + build | Run broader frontend coverage. | |
| Browser QA | Add manual browser verification. | |

---

## the agent's Discretion

- Exact numeric values for sitemap release limits.
- Exact route-level metadata test fixture and mock details.
- Minor placement variations if Next.js/Vitest route test constraints require them.

## Deferred Ideas

- Segmented sitemap files via Next.js `generateSitemaps` when catalog size exceeds the Phase 4 single-sitemap release limit.
- SEO/GEO landing pages.
- Broad frontend/backend contract drift checks.
- Production deployment documentation and final release audit.
