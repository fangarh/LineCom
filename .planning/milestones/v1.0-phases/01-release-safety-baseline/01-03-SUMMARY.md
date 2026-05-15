---
phase: 01-release-safety-baseline
plan: "03"
subsystem: frontend-api
tags: [nextjs, vitest, fetch, api-client, transport-errors]
requires: []
provides:
  - Shared frontend API response parsing for JSON and multipart requests
  - Controlled `transport.invalid_response` errors for non-JSON, empty, and malformed responses
  - Diagnostic metadata for invalid upstream responses without exposing raw details to users
affects: [release-safety-baseline, frontend-api, admin-catalog]
tech-stack:
  added: []
  patterns:
    - Shared `parseApiResponse` path for `apiJson` and `apiForm`
    - `ApiClientError` diagnostic metadata kept separate from normalized user-facing messages
key-files:
  created:
    - apps/front/src/lib/api/http.test.ts
  modified:
    - apps/front/src/lib/api/http.ts
    - apps/front/src/lib/api/errors.ts
    - apps/front/src/lib/api/errors.test.ts
    - apps/front/src/lib/api/admin-catalog.test.ts
key-decisions:
  - "Preserved the existing `internal_error` fallback for syntactically valid JSON error bodies that do not match `{ code, message }`."
  - "Applied `transport.invalid_response` to non-JSON, empty, and malformed responses for both JSON and multipart helpers."
patterns-established:
  - "Frontend transport parsing failures should throw `ApiClientError` with stable code/message plus optional diagnostics."
requirements-completed: [API-01, API-02]
duration: 4 min
completed: 2026-05-14
---

# Phase 01 Plan 03: Frontend API Transport Error Normalization Summary

**Shared frontend API parsing turns malformed upstream responses into stable `transport.invalid_response` client errors**

## Performance

- **Duration:** 4 min
- **Started:** 2026-05-14T15:53:30Z
- **Completed:** 2026-05-14T15:57:41Z
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments

- Refactored `apiJson` and `apiForm` to use one shared `parseApiResponse` helper.
- Added `transport.invalid_response` with the stable retry message `Не удалось обработать ответ сервера. Попробуйте позже.`
- Extended `ApiClientError` with diagnostic metadata for invalid response details while keeping `normalizeApiError` user-safe.
- Added Vitest coverage for non-JSON, empty, malformed, valid backend API error, no-content, JSON helper, and multipart helper paths.

## Task Commits

1. **Task 1: Add shared response parsing helper for JSON and form requests** - `d094502` (fix)
2. **Task 2: Preserve diagnostics without exposing raw details to users** - `d094502` (fix)
3. **Task 3: Add JSON and multipart invalid-response tests** - `d094502` (fix)

**Plan metadata:** included in `docs(01-03): complete API transport error plan`

## Files Created/Modified

- `apps/front/src/lib/api/http.ts` - Shared parsing helper used by `apiJson` and `apiForm`.
- `apps/front/src/lib/api/errors.ts` - Invalid response API error constant and diagnostics metadata support.
- `apps/front/src/lib/api/errors.test.ts` - Normalization test proving diagnostics are not exposed to users.
- `apps/front/src/lib/api/http.test.ts` - JSON/form transport error regression tests.
- `apps/front/src/lib/api/admin-catalog.test.ts` - Multipart wrapper coverage for invalid transport responses.

## Decisions Made

- Kept the existing `internal_error` fallback for valid JSON error bodies with unknown shape, because Phase 1 decisions target non-JSON, empty, and malformed transport responses.

## Deviations from Plan

None - plan executed within the planned API client and test scope.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No public UI surface exposes raw proxy HTML, plaintext upstream body, or JSON parser details.

## Issues Encountered

- `npm --prefix apps/front run lint` completed with exit code 0 and one unrelated warning in `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`, which was already outside this plan's scope.

## Verification

- `npm --prefix apps/front test -- src/lib/api/http.test.ts src/lib/api/errors.test.ts src/lib/api/admin-catalog.test.ts`: passed, 3 files and 20 tests.
- `npm --prefix apps/front test -- src/lib/api`: passed, 6 files and 33 tests.
- `npm --prefix apps/front run lint`: passed with 0 errors and 1 unrelated warning.
- Acceptance inspection: `apiJson` and `apiForm` both call `parseApiResponse`; only the shared helper owns `JSON.parse`; tests assert `transport.invalid_response` and the stable retry message.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Wave 1 is complete. Phase 1 can continue to Wave 2 plan `01-02`, which depends on the completed `01-01` `Program.cs` baseline.

---
*Phase: 01-release-safety-baseline*
*Completed: 2026-05-14*
