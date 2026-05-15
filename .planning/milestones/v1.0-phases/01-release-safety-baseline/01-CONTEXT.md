# Phase 1: Release Safety Baseline - Context

**Gathered:** 2026-05-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 1 delivers release-safety baseline work only: auth abuse protection for login/register, production-safe public SEO origin validation, and controlled frontend API error normalization. This phase does not add new product capabilities and does not change Local FileStorage, sitemap scaling, admin maintainability, product comparison, landing pages, or import/export flows.

</domain>

<decisions>
## Implementation Decisions

### Auth throttling
- **D-01:** Apply throttling only to `POST /api/auth/login` and `POST /api/auth/register` in Phase 1.
- **D-02:** Use `IP + endpoint` as the throttling key for this phase.
- **D-03:** Start with a limit of `5 attempts / 1 minute`.
- **D-04:** When throttled, return HTTP `429` with JSON API error `{ code: "auth.rate_limited", message: "Слишком много попыток. Попробуйте позже." }`.
- **D-05:** Do not include `/api/auth/me`, `/api/auth/logout`, or request submit throttling in this phase.

### Production origin guards
- **D-06:** Add a frontend production build/startup guard for `LINECOM_PUBLIC_SITE_ORIGIN`.
- **D-07:** In production, treat missing value, invalid URL, `localhost`, `127.0.0.1`, and `::1` as invalid public site origins.
- **D-08:** Do not hard-require `https://line-com.ru`; staging/preview domains may be valid if they are absolute non-localhost URLs.
- **D-09:** Do not tighten `LINECOM_API_ORIGIN` in Phase 1; keep the existing frontend fallback behavior for server-side API origin.
- **D-10:** Bad public origin should fail fast with a clear message and example, e.g. `LINECOM_PUBLIC_SITE_ORIGIN must be an absolute non-localhost URL in production, e.g. https://line-com.ru`.

### API error normalization
- **D-11:** If backend/proxy returns non-JSON, empty error body, or malformed JSON, frontend API helpers should throw `ApiClientError` with code `transport.invalid_response`.
- **D-12:** User-facing message for invalid responses: `Не удалось обработать ответ сервера. Попробуйте позже.`
- **D-13:** Preserve technical details for diagnostics, but do not show them to users. Planner may choose a safe mechanism such as `cause`, dev-only logging, or test-visible metadata.
- **D-14:** Apply the same parsing behavior to both `apiJson` and `apiForm` through a shared parsing helper.

### the agent's Discretion
- Exact ASP.NET Core rate limiter policy names and registration structure.
- Whether the public origin guard runs through direct helper validation, Next config evaluation, or a small shared config validation module, as long as it fails during production build/startup.
- Exact diagnostic mechanism for invalid response details, provided production UI does not expose raw HTML/plaintext/parse errors.

</decisions>

<specifics>
## Specific Ideas

- Keep error responses aligned with the existing `{ code, message }` API error model.
- Preserve local development defaults where explicitly allowed: `LINECOM_API_ORIGIN` fallback remains untouched in Phase 1.
- The public site origin guard should protect canonical URLs, `metadataBase`, robots and sitemap output from localhost leakage.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and phase scope
- `.planning/PROJECT.md` - project context, core value, constraints and active release-stabilization scope.
- `.planning/REQUIREMENTS.md` - Phase 1 requirements `SEC-01`, `SEC-02`, `PROD-01`, `PROD-03`, `API-01`, `API-02`.
- `.planning/ROADMAP.md` - Phase 1 goal, plans and success criteria.
- `.planning/research/SUMMARY.md` - release-stabilization research summary.

### Codebase map
- `.planning/codebase/ARCHITECTURE.md` - backend/frontend module boundaries and integration points.
- `.planning/codebase/CONCERNS.md` - known risks for auth throttling, production SEO origin and API transport parsing.
- `.planning/codebase/TESTING.md` - xUnit/Vitest test patterns and commands.

### Backend auth and hosting
- `apps/api/Program.cs` - middleware pipeline, forwarded headers, HTTPS policy, auth registration and controller mapping.
- `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs` - cookie authentication settings and JSON auth error responses.
- `apps/api/Modules/Auth/Controllers/AuthController.cs` - `register`, `login`, `me`, `logout` endpoints.

### Frontend API and SEO origin
- `apps/front/src/lib/api/http.ts` - `apiJson`, `apiForm`, JSON parsing and server-side API origin fallback.
- `apps/front/src/lib/api/errors.ts` - `ApiClientError`, API error shape and normalization helpers.
- `apps/front/src/lib/seo/site.ts` - `LINECOM_PUBLIC_SITE_ORIGIN` normalization and localhost fallback.
- `apps/front/src/lib/seo/metadata.ts` - canonical metadata helpers.
- `apps/front/src/app/robots.ts` - robots route using absolute site URL.
- `apps/front/src/app/sitemap.ts` - sitemap route using public site origin and public catalog API.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ApiErrorResponse` and `ApiExceptionMiddleware` already establish the JSON shape `{ code, message }` for backend errors.
- `AuthServiceCollectionExtensions.WriteAuthErrorAsync` already serializes auth-related JSON errors and can guide `429` body shape.
- Frontend `ApiClientError` already carries status and API error response; invalid response handling should reuse this class instead of creating a parallel error type.
- Existing Vitest tests under `apps/front/src/lib/api` and xUnit endpoint tests under `tests/LineCom.Api.Tests/Modules/Auth` provide the likely test locations.

### Established Patterns
- Backend modules register dependencies through `*ServiceCollectionExtensions.cs`.
- Controllers stay thin; middleware and filters should handle cross-cutting concerns where appropriate.
- Frontend API access goes through typed modules in `apps/front/src/lib/api`, with shared behavior centralized in `http.ts`.
- SEO helpers centralize site origin and canonical URL behavior in `apps/front/src/lib/seo`.

### Integration Points
- Auth throttling integrates with `apps/api/Program.cs`, `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`, and `apps/api/Modules/Auth/Controllers/AuthController.cs`.
- Public origin guard integrates with `apps/front/src/lib/seo/site.ts`, `apps/front/src/app/layout.tsx`, `apps/front/src/app/robots.ts`, and `apps/front/src/app/sitemap.ts`.
- API invalid response normalization integrates with `apps/front/src/lib/api/http.ts` and tests for both JSON and multipart request helpers.

</code_context>

<deferred>
## Deferred Ideas

None - discussion stayed within Phase 1 scope.

</deferred>

---

*Phase: 01-release-safety-baseline*
*Context gathered: 2026-05-14*
