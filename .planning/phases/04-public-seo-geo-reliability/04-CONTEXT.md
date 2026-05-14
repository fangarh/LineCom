# Phase 4: Public SEO/GEO Reliability - Context

**Gathered:** 2026-05-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 4 delivers reliability for the existing public SEO/GEO surface: canonical metadata for representative catalog and product routes, production-safe `robots.ts` and `sitemap.ts` output, and a bounded sitemap strategy that is documented and test-covered.

This phase is limited to existing public routes and SEO/GEO infrastructure: `/catalog`, `/catalog/{categorySlug}`, `/products/{slug}`, root metadata, `robots.ts`, `sitemap.ts`, `apps/front/src/lib/seo/*`, and public catalog API SEO fields. It does not add SEO landing pages, product comparison, admin maintainability work, broad frontend/backend contract testing, production deployment documentation, or a new backend sitemap feed unless planning proves it is the smallest bounded implementation.

</domain>

<decisions>
## Implementation Decisions

### Sitemap scaling strategy
- **D-01:** Keep Phase 4 on a single public `/sitemap.xml`; do not implement segmented sitemap files in this phase.
- **D-02:** Make sitemap product enumeration explicitly bounded with a combined release limit: maximum product API pages and maximum product URLs.
- **D-03:** If the catalog exceeds the Phase 4 single-sitemap release limit, sitemap generation must return a valid truncated sitemap rather than fail or enumerate without bound.
- **D-04:** The truncation behavior and release limit must be documented as a Phase 4 bounded strategy. Segmented sitemaps via Next.js `generateSitemaps` are the future path when the catalog grows beyond this limit.
- **D-05:** Sitemap limit constants should live near `apps/front/src/app/sitemap.ts` and be covered by route-level tests. Do not introduce env/config surface for these release limits in Phase 4.

### Canonical metadata verification
- **D-06:** Add route-level metadata tests for representative category and product happy paths.
- **D-07:** Category metadata tests must prove `/catalog/{categorySlug}` uses the public API `seo.canonicalPath`, title and description, and returns indexable robots metadata for valid API data.
- **D-08:** Product metadata tests must prove `/products/{slug}` uses the public API `seo.canonicalPath`, title and description, and returns indexable robots metadata for valid API data.
- **D-09:** The frontend must not rebuild canonical category/product URLs independently when API-backed `canonicalPath` exists; the public API remains the source of truth for catalog entity canonical paths.
- **D-10:** Place route-level metadata tests beside the route files, for example `apps/front/src/app/catalog/[categorySlug]/page.test.tsx` and `apps/front/src/app/products/[slug]/page.test.tsx`.
- **D-11:** Existing API failure/noindex fallback behavior for public metadata is considered sufficient; Phase 4 should not add new fallback logic unless tests expose a regression.

### Robots and sitemap references
- **D-12:** Keep `robots.ts` pointing to a single absolute `/sitemap.xml` URL because Phase 4 keeps a bounded single sitemap.
- **D-13:** Do not add future segmented sitemap URLs to `robots.ts` before segmented sitemap files exist.
- **D-14:** Required `robots.ts` assertions are: production-safe absolute `sitemap`, normalized `host`, `allow: "/"`, and `disallow` entries for `/admin/`, `/account/`, and `/auth/`.
- **D-15:** Prefer tests first for `robots.ts`; change production code only if the tests reveal a gap.

### SEO/GEO regression gate
- **D-16:** The Phase 4 regression gate is a focused automated test surface plus phase documentation, not a broad contract-test framework.
- **D-17:** SEO/GEO-sensitive changes include public routes (`/catalog`, `/catalog/{slug}`, `/products/{slug}`), route metadata, root `metadataBase`, `robots.ts`, `sitemap.ts`, `apps/front/src/lib/seo/*`, and public catalog API SEO fields (`seo.canonicalPath`, `seo.title`, `seo.description`).
- **D-18:** Future changes to the SEO/GEO-sensitive surface must update the relevant metadata, robots, sitemap or route-level tests.
- **D-19:** Phase 4 completion evidence should include a focused Vitest suite for SEO metadata, sitemap, robots and new route metadata tests, plus `npm run build` for the frontend.

### the agent's Discretion
- Exact numeric values for `SITEMAP_MAX_PRODUCT_PAGES` and `SITEMAP_MAX_PRODUCT_URLS`, provided they make enumeration bounded, are clearly named, and tests prove truncation.
- Exact test fixture shapes and mocking strategy for route-level metadata tests, provided they stay close to existing Vitest patterns and do not require live backend data.
- Exact route-level test filenames if framework constraints require a small variation, provided the tests stay beside the route files.
- Whether sitemap truncation is implemented directly in `app/sitemap.ts` or with a small local helper, provided the release limit constants remain visible near the route and no broad abstraction is introduced.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and phase scope
- `.planning/PROJECT.md` - project constraints, SEO/GEO source-of-truth rule, validated prior phases and dirty-worktree constraint.
- `.planning/REQUIREMENTS.md` - Phase 4 requirements `SEO-01`, `SEO-02`, `SEO-03`.
- `.planning/ROADMAP.md` - Phase 4 goal, success criteria and planned split `04-01`/`04-02`/`04-03`.
- `.planning/STATE.md` - current workflow position and session state.

### Prior phase decisions and verification
- `.planning/phases/01-release-safety-baseline/01-CONTEXT.md` - production public origin decisions for `LINECOM_PUBLIC_SITE_ORIGIN`.
- `.planning/phases/01-release-safety-baseline/01-VERIFICATION.md` - confirms production origin guardrails and frontend API error normalization were verified.
- `.planning/phases/02-storage-access-and-diagnostics/02-CONTEXT.md` - confirms storage/public path boundaries remain out of Phase 4 scope.
- `.planning/phases/03-import-storage-consistency/03-VERIFICATION.md` - confirms Phase 3 import storage work is complete and unrelated to Phase 4 implementation.

### Codebase map
- `.planning/codebase/ARCHITECTURE.md` - Next.js App Router public SEO pages, frontend SEO helpers and public catalog API flow.
- `.planning/codebase/CONCERNS.md` - sitemap product enumeration scaling concern and SEO/GEO fragile-area notes.
- `.planning/codebase/TESTING.md` - Vitest, route/helper test organization and frontend verification commands.
- `.planning/codebase/STACK.md` - Next.js 16.2.4, React 19 and frontend build/runtime context.
- `.planning/codebase/CONVENTIONS.md` - frontend SEO/GEO conventions and route/helper organization.

### Source-of-truth product docs
- `vault/Человекочитаемое/SEO GEO Public Catalog.md` - existing public SEO/GEO contract for indexable pages, canonical paths, robots and sitemap.
- `vault/Человекочитаемое/SEO GEO Public Catalog handoff.md` - previous SEO/GEO implementation handoff, existing checks, and unverified API-backed canonical note.
- `vault/Человекочитаемое/Сквозные требования.md` - cross-cutting SEO/GEO requirement and no-intentional-technical-debt rule.
- `vault/Человекочитаемое/Public Catalog API.md` - public catalog API contract if planning needs to confirm SEO fields.

### Frontend SEO implementation
- `apps/front/src/lib/seo/site.ts` - public origin normalization, metadata base and absolute URL helpers.
- `apps/front/src/lib/seo/site.test.ts` - production origin and absolute URL tests from Phase 1.
- `apps/front/src/lib/seo/metadata.ts` - `indexablePageMetadata` and `noindexPageMetadata`.
- `apps/front/src/lib/seo/metadata.test.ts` - helper-level canonical/noindex metadata tests.
- `apps/front/src/lib/seo/sitemap.ts` - sitemap entry builder, category flattening and URL deduplication.
- `apps/front/src/lib/seo/sitemap.test.ts` - sitemap builder tests.
- `apps/front/src/app/layout.tsx` - root metadata and `metadataBase`.
- `apps/front/src/app/sitemap.ts` - current sitemap route and product enumeration behavior.
- `apps/front/src/app/sitemap.test.ts` - current route-level sitemap tests and fallback behavior.
- `apps/front/src/app/robots.ts` - current robots route.
- `apps/front/src/app/robots.test.ts` - current robots route tests.
- `apps/front/src/app/catalog/[categorySlug]/page.tsx` - category route `generateMetadata` and page data behavior.
- `apps/front/src/app/products/[slug]/page.tsx` - product route `generateMetadata` and page data behavior.
- `apps/front/src/lib/api/catalog.ts` - frontend public catalog DTOs and fetch wrappers, including `seo` fields.

### Backend public catalog implementation
- `apps/api/Modules/Catalog/DTOs/PublicCatalogSharedDtos.cs` - public SEO DTO shape.
- `apps/api/Modules/Catalog/DTOs/PublicCategoryDtos.cs` - public category DTOs.
- `apps/api/Modules/Catalog/DTOs/PublicProductDtos.cs` - public product DTOs.
- `apps/api/Modules/Catalog/Queries/PublicCategoryDetailBuilder.cs` - category canonical path builder.
- `apps/api/Modules/Catalog/Queries/PublicProductDetailResponseBuilder.cs` - product canonical path builder.
- `apps/api/Modules/Catalog/Queries/PublicCategorySql.cs` - category SEO field selection.
- `apps/api/Modules/Catalog/Queries/PublicProductSql.cs` - product SEO field selection.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `normalizeSiteOrigin`, `getPublicSiteOrigin`, `siteMetadataBase` and `absoluteSiteUrl` already centralize public origin behavior in `apps/front/src/lib/seo/site.ts`.
- `indexablePageMetadata` already creates canonical/indexable metadata and should remain the route-level canonical entry point.
- `buildPublicSitemapEntries` already deduplicates static, category and product URLs and can remain the final builder after bounded product loading.
- `app/sitemap.test.ts` already mocks `getCategoryTree`, `getProducts` and public origin, making it the natural place to prove bounded enumeration.
- `robots.test.ts` already covers the current robots shape and can be strengthened without broad refactor.
- Category and product route `generateMetadata` functions are direct test targets for API-backed canonical behavior.

### Established Patterns
- Public App Router pages fetch data server-side and use API-provided SEO values for category/product metadata.
- Static public pages and entity pages use relative canonical paths resolved by root `metadataBase`.
- Unavailable public category/product metadata falls back to noindex metadata; 404 API errors are handled by `notFound()` in page loading.
- Frontend tests use Vitest with module mocks near implementation files.
- SEO/GEO changes must preserve route-level metadata, robots, sitemap and public API SEO field behavior.

### Integration Points
- `apps/front/src/app/sitemap.ts` needs bounded product loading before calling `buildPublicSitemapEntries`.
- `apps/front/src/app/robots.ts` must continue using `absoluteSiteUrl("/sitemap.xml")` and `getPublicSiteOrigin()`.
- `apps/front/src/app/catalog/[categorySlug]/page.tsx` and `apps/front/src/app/products/[slug]/page.tsx` need route-level tests around `generateMetadata`.
- `apps/front/src/lib/api/catalog.ts` and backend public catalog DTO/builders are the contract boundary for `seo.canonicalPath`, `seo.title` and `seo.description`.

</code_context>

<specifics>
## Specific Ideas

- Current Next.js documentation confirms `generateSitemaps` is the supported path for splitting large sitemap datasets and that sitemap metadata routes are cached by default unless dynamic request-time behavior is introduced. Phase 4 deliberately does not adopt segmentation yet; it documents segmented sitemaps as the future growth path.
- Use a small, visible release limit rather than hidden config so Phase 4 remains a release-stabilization change with deterministic tests.
- Avoid browser QA as a required Phase 4 completion gate unless planning finds it cheap and stable with local API/data; route-level tests plus frontend build are the required evidence.

</specifics>

<deferred>
## Deferred Ideas

- Segmented sitemap files via Next.js `generateSitemaps` when the catalog grows beyond the Phase 4 single-sitemap release limit.
- SEO/GEO landing pages for category/filter/brand/region combinations; this remains v2 product expansion scope.
- Broad frontend/backend contract drift checks; this belongs to Phase 5 admin maintainability/contracts, not Phase 4.
- Production deployment documentation and final release audit; this belongs to Phase 6 production readiness.

</deferred>

---

*Phase: 04-Public SEO/GEO Reliability*
*Context gathered: 2026-05-14*
