---
phase: 01-release-safety-baseline
status: passed
verified: 2026-05-14
requirements:
  - SEC-01
  - SEC-02
  - PROD-01
  - PROD-03
  - API-01
  - API-02
source:
  - 01-01-PLAN.md
  - 01-02-PLAN.md
  - 01-03-PLAN.md
  - 01-01-SUMMARY.md
  - 01-02-SUMMARY.md
  - 01-03-SUMMARY.md
---

# Phase 01 Verification: Release Safety Baseline

## Verdict

**Status:** passed

Phase 1 goal is achieved: login/register abuse protection, production origin validation, production environment guardrails, auth cookie verification, and controlled frontend API transport errors are implemented with automated coverage.

## Requirement Coverage

| Requirement | Status | Evidence |
|-------------|--------|----------|
| SEC-01 | passed | `AuthRateLimiting` policy limits `POST /api/auth/login` and `POST /api/auth/register`; endpoint tests assert sixth attempt returns 429 with `auth.rate_limited`. |
| SEC-02 | passed | `AuthModuleRegistrationTests` verify production cookie options: HttpOnly, SameSite=Lax, SecurePolicy=Always; non-production remains SameAsRequest. |
| PROD-01 | passed | `site.ts` rejects missing/invalid/localhost `LINECOM_PUBLIC_SITE_ORIGIN` in production; `next build` fails with the expected message when origin is missing. |
| PROD-03 | passed | API `ProductionConfigurationGuard` rejects blank production DB connection and blank/relative `Storage:RootPath`; frontend public origin is production-validated. |
| API-01 | passed | `apiJson` and `apiForm` share `parseApiResponse` and throw `ApiClientError` code `transport.invalid_response` for non-JSON, empty, and malformed responses. |
| API-02 | passed | Vitest coverage includes JSON and multipart invalid-response paths, backend API error preservation, 204 handling, and diagnostics normalization. |

## Must-Have Checks

### Auth Rate Limiting

- `POST /api/auth/login` and `POST /api/auth/register` are the only auth endpoints with `EnableRateLimiting`.
- Rate limiting key combines remote IP and endpoint path.
- Fixed window is `5 attempts / 1 minute` with `QueueLimit = 0`.
- Rejection response is JSON `{ code: "auth.rate_limited", message: "Слишком много попыток. Попробуйте позже." }`.
- `/api/auth/me`, `/api/auth/logout`, and request submit endpoints were not changed for throttling.

### Production Origin And Environment Guardrails

- Production frontend rejects missing, invalid URL, `localhost`, `127.0.0.1`, and `[::1]` public site origins.
- Production frontend accepts absolute non-localhost origins such as `https://line-com.ru` and `https://preview.example.ru`.
- `apps/front/next.config.ts` still contains `LINECOM_API_ORIGIN ?? "http://127.0.0.1:8080"`.
- API production guard is invoked before `AddDatabase`.
- No `/storage` public/private serving policy was changed; Phase 2 scope remains untouched.

### Frontend API Transport Errors

- `apiJson` and `apiForm` call the same parsing helper.
- Non-JSON, empty, and malformed non-204 responses throw `transport.invalid_response`.
- Valid backend `{ code, message }` errors keep their backend code/message.
- Raw proxy HTML, plaintext body, and JSON parse messages are retained only in diagnostics and are not returned by `normalizeApiError`.

## Automated Checks

| Command | Result |
|---------|--------|
| `dotnet test LineCom.sln --no-restore --filter "FullyQualifiedName~Auth\|FullyQualifiedName~ProductionConfigurationGuard"` | passed, 76/76 tests |
| `npm --prefix apps/front test -- src/lib/api src/lib/seo/site.test.ts` | passed, 7 files and 43 tests |
| `npm --prefix apps/front run lint` | passed with 0 errors and 1 unrelated warning in `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx` |
| `npm --prefix apps/front run build` without `LINECOM_PUBLIC_SITE_ORIGIN` | failed as expected with `LINECOM_PUBLIC_SITE_ORIGIN must be an absolute non-localhost URL in production, e.g. https://line-com.ru` |
| `npm --prefix apps/front run build` with `LINECOM_PUBLIC_SITE_ORIGIN=https://line-com.ru` | passed |
| `gsd-sdk.cmd query verify.schema-drift "01"` | passed, no schema drift detected |

## Residual Risks

- NuGet vulnerability metadata warnings appeared because `https://api.nuget.org/v3/index.json` was unavailable in this environment. This is not a Phase 1 functional failure; dependency audit remains tracked by Phase 6 requirement `SEC-03`.
- One frontend lint warning exists in an unrelated pre-existing dirty file outside Phase 1 scope: `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`.

## Human Verification

No manual human verification is required for Phase 1. Automated checks cover the phase must-haves.

---
*Verified: 2026-05-14*
