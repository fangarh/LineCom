# Phase 1: Release Safety Baseline - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-05-14
**Phase:** 1-Release Safety Baseline
**Areas discussed:** Auth throttling, Production origin guards, API error normalization

---

## Auth throttling

| Question | Options Considered | User's Choice |
|----------|--------------------|---------------|
| Which endpoints are limited in Phase 1? | `login + register`; `login + register + request submit`; all auth endpoints; agent decides | `login + register` |
| What throttling key should be used? | `IP + endpoint`; `login/email/phone + IP`; global endpoint limit; agent decides | `IP + endpoint` |
| What initial limit should be used? | `5 attempts / 1 minute`; `10 attempts / 1 minute`; `5 attempts / 5 minutes`; agent decides | `5 attempts / 1 minute` |
| What response should the client receive? | `429 auth.rate_limited`; `429 too_many_requests`; `429` without body; agent decides | `429 auth.rate_limited` |

**Notes:** Scope intentionally stays on public login/register endpoints. Request submit and account-specific throttling are deferred outside Phase 1.

---

## Production origin guards

| Question | Options Considered | User's Choice |
|----------|--------------------|---------------|
| Where should bad production origin be rejected? | frontend build/startup guard; deploy-check only; runtime noindex fallback; agent decides | frontend build/startup guard |
| Which values are invalid in production? | empty/invalid/localhost; only not `https://line-com.ru`; any `http://`; agent decides | empty/invalid/localhost |
| What about `LINECOM_API_ORIGIN`? | validate too; leave as-is; remove fallback in all envs; agent decides | leave as-is |
| What error style should be used? | fail-fast clear message; silently replace with `https://line-com.ru`; warning only; agent decides | fail-fast clear message |

**Notes:** Phase 1 protects public SEO origin only. `LINECOM_API_ORIGIN` fallback remains unchanged.

---

## API error normalization

| Question | Options Considered | User's Choice |
|----------|--------------------|---------------|
| What code should invalid backend/proxy responses use? | `transport.invalid_response`; `internal_error`; `api.invalid_json`; agent decides | `transport.invalid_response` |
| What user message should be shown? | `Не удалось обработать ответ сервера. Попробуйте позже.`; `Внутренняя ошибка сервера.`; `Сервер вернул некорректный ответ.`; agent decides | `Не удалось обработать ответ сервера. Попробуйте позже.` |
| Should technical details be preserved for development? | yes in diagnostic field/cause; no only public error; show details in development UI; agent decides | yes in diagnostic field/cause |
| Should `apiJson` and `apiForm` be covered equally? | shared parsing helper; only `apiJson`; separate same logic; agent decides | shared parsing helper |

**Notes:** Raw parse errors should not leak to UI. Diagnostics may be preserved safely for development and tests.

---

## the agent's Discretion

- Exact ASP.NET Core rate limiter policy names and registration structure.
- Exact frontend guard implementation shape, as long as production build/startup fails for invalid public origin.
- Exact diagnostic mechanism for invalid response details, as long as users do not see raw technical payloads.

## Deferred Ideas

None.
