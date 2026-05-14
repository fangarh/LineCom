# Research: Features

**Date:** 2026-05-14

## Table Stakes For Release Stabilization

- Auth endpoint abuse protection: login/register throttling, predictable 429 behavior and tests.
- Production configuration validation: public site origin, API origin, HTTPS/proxy assumptions and storage root checks.
- API error normalization: non-JSON upstream/proxy failures should still surface as controlled frontend errors.
- Storage access boundary: public catalog images should be served, non-public import/export/temp artifacts should not leak.
- Storage lifecycle diagnostics: detect missing files, untracked files, stale deleted/orphaned rows and backup/restore risks.
- SEO/GEO validation: canonical URLs, robots, sitemap, noindex fallbacks and route metadata must be checked in release gates.
- Contract drift checks: critical frontend API clients should be protected against backend DTO/endpoint drift.

## Differentiators Deferred Beyond Stabilization

- Product comparison by normalized attributes.
- SEO/GEO landing pages for filter/category/brand/region combinations.
- Web-based import/export workflow with mapping persistence and row-level error review.
- More advanced catalog search/scaling work such as segmented sitemap snapshots or dedicated backend sitemap feed.

## Anti-Features For Current Milestone

- Replacing Local FileStorage with object storage.
- Introducing Entity Framework for schema or data access.
- Adding online payment or paid-order checkout.
- Expanding public pricing or exact stock exposure.
- Adding large new admin catalog behavior before decomposing fragile containers.

## Complexity Notes

- Storage hardening crosses database schema, API static serving, filesystem operations, import tooling and operational docs.
- SEO/GEO work crosses frontend route files, metadata helpers, environment configuration and public catalog API behavior.
- Auth hardening is smaller, but security-sensitive and needs precise tests around throttling and cookies.
