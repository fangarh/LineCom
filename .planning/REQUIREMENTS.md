# Requirements: LineCom Release Stabilization

**Defined:** 2026-05-14
**Core Value:** Покупатель должен находить нужные кабельные товары через SEO/GEO-доступный каталог и надежно отправлять коммерческую заявку, которую продавец может обработать без потери данных.

## v1 Requirements

### Security

- [x] **SEC-01**: Public login and registration endpoints are protected by rate limiting or equivalent throttling with tested 429 behavior.
- [x] **SEC-02**: Cookie authentication settings are verified for production HTTPS, HttpOnly, Secure and SameSite behavior.
- [x] **SEC-03**: Release verification includes dependency vulnerability audit for .NET and npm packages when network access is available.

### Production Configuration

- [x] **PROD-01**: Production startup or build checks fail clearly when public site origin or API origin configuration would generate localhost SEO URLs.
- [x] **PROD-02**: Deployment documentation and verification cover API, frontend, DbUp migrator, PostgreSQL and Local FileStorage paths.
- [x] **PROD-03**: Release checks validate that storage root, database connection and frontend/backend origins are environment-specific and not silently defaulting in production.

### API Error Handling

- [x] **API-01**: Frontend API transport normalizes non-JSON, empty and malformed error responses into controlled client errors.
- [x] **API-02**: API error handling tests cover proxy/upstream-style failures for JSON and multipart requests.

### Storage Lifecycle

- [x] **STOR-01**: Public static storage serving is limited to intended public catalog image paths or enforced through an access-checking controller.
- [x] **STOR-02**: Non-public file purposes such as import source, export result and temp artifacts cannot be fetched anonymously through `/storage`.
- [x] **STOR-03**: Storage diagnostics report missing files, untracked files, stale deleted/orphaned rows and database/file drift.
- [x] **STOR-04**: Catalog import file writes use a documented staging/commit/cleanup model or equivalent compensation path.
- [x] **STOR-05**: Backup and restore expectations for Local FileStorage are documented and included in release verification.

### SEO/GEO

- [x] **SEO-01**: Public catalog, product, robots and sitemap routes generate production-safe canonical URLs from validated site origin.
- [x] **SEO-02**: Sitemap generation is protected against unbounded per-request product enumeration or has a clear segmented/cached strategy.
- [x] **SEO-03**: SEO/GEO route tests cover metadata, canonical URLs, robots and sitemap behavior for representative catalog/product pages.

### Maintainability

- [x] **MAIN-01**: Large admin catalog/homepage frontend containers touched by release work are decomposed before new behavior is added.
- [x] **MAIN-02**: Pure mapping, payload building and normalization logic extracted from admin UI has focused unit tests.
- [x] **MAIN-03**: Critical frontend API clients have contract checks or tests that detect backend DTO/endpoint drift.

### Verification

- [x] **VER-01**: Each release-stabilization phase has explicit success criteria tied to tests or manual verification steps.
- [x] **VER-02**: Before a phase is marked complete, GSD verifier checks technical debt, security gaps, migration risks and maintainability risks.

## v2 Requirements

### Product Expansion

- **COMP-01**: User can add compatible products to comparison from listing and product detail pages.
- **COMP-02**: User can compare products by normalized category attributes without public price comparison.
- **LAND-01**: Admin can manage SEO/GEO landing pages for approved category/filter/brand/region combinations.
- **LAND-02**: Public landing pages expose canonical, metadata, sitemap and noindex behavior according to SEO/GEO rules.
- **IMEX-01**: Admin can run web-based catalog import/export with mapping persistence and row-level error review.
- **IMEX-02**: Import/export artifacts follow the hardened Local FileStorage lifecycle.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Online payment | Release model processes payment and legal order fixation outside the site. |
| Public prices | Product model uses "Цена по запросу". |
| Exact stock balances | Release model exposes coarse availability only. |
| Entity Framework | Project standard is Npgsql/Dapper with DbUp SQL migrations. |
| Object storage migration | Local FileStorage is the target approach. |
| New product features before stabilization | Current milestone prioritizes release hardening before scope expansion. |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEC-01 | Phase 1 | Complete |
| SEC-02 | Phase 1 | Complete |
| SEC-03 | Phase 6 | Complete |
| PROD-01 | Phase 1 | Complete |
| PROD-02 | Phase 6 | Complete |
| PROD-03 | Phase 1 | Complete |
| API-01 | Phase 1 | Complete |
| API-02 | Phase 1 | Complete |
| STOR-01 | Phase 2 | Complete |
| STOR-02 | Phase 2 | Complete |
| STOR-03 | Phase 2 | Complete |
| STOR-04 | Phase 3 | Complete |
| STOR-05 | Phase 6 | Complete |
| SEO-01 | Phase 4 | Complete |
| SEO-02 | Phase 4 | Complete |
| SEO-03 | Phase 4 | Complete |
| MAIN-01 | Phase 5 | Complete |
| MAIN-02 | Phase 5 | Complete |
| MAIN-03 | Phase 5 | Complete |
| VER-01 | Phase 6 | Complete |
| VER-02 | Phase 6 | Complete |

**Coverage:**
- v1 requirements: 21 total
- Mapped to phases: 21
- Unmapped: 0

---
*Requirements defined: 2026-05-14*
*Last updated: 2026-05-14 after GSD initialization*
