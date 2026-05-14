# Research: Stack

**Date:** 2026-05-14

## Current Stack

LineCom already has a settled brownfield stack:

- Backend: ASP.NET Core on .NET 8 in `apps/api`.
- Data access: PostgreSQL through Npgsql and Dapper.
- Migrations: DbUp with SQL scripts in `apps/dbmigrator/Migrations`.
- Frontend: Next.js App Router, React, TypeScript in `apps/front`.
- Tests: xUnit for .NET code and Vitest/testing-library for frontend code.
- Storage: Local FileStorage served by the API under `/storage`.
- Import tooling: .NET core importer plus WinForms UI.

## External Documentation Findings

ASP.NET Core official docs support using `AddRateLimiter` plus `UseRateLimiter` and endpoint policies such as fixed-window or concurrency limiters for targeted throttling. For LineCom this maps directly to login/register and possibly request-submit endpoints.

ASP.NET Core cookie guidance emphasizes secure cookies over HTTPS, `HttpOnly`, explicit `SameSite` choices and correct middleware order: routing, CORS where applicable, authentication, authorization, endpoints.

Next.js App Router documentation supports `metadataBase`, `alternates.canonical`, `robots.ts` and `sitemap.ts` as first-class APIs. For LineCom, production origin validation is important because canonical URLs, robots and sitemap should not silently fall back to localhost.

## Prescriptive Stack Decisions

- Keep ASP.NET Core + Npgsql/Dapper + DbUp; do not introduce Entity Framework.
- Keep Next.js App Router for public SEO/GEO routes.
- Keep Local FileStorage and harden it rather than replacing it.
- Add rate limiting through ASP.NET Core's built-in middleware before building custom throttling infrastructure.
- Keep explicit SQL and add targeted migration/database tests for storage/security/index changes.

## Confidence

High. These choices are already established in `vault/Человекочитаемое`, the solution structure and `.planning/codebase/STACK.md`.
