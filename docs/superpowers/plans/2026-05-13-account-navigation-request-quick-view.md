# Account Navigation + Request Quick View Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make post-login navigation role-aware, add secure logout, and add quick request preview to customer and admin request lists.

**Architecture:** Keep customer and admin request flows separated. The header restores auth state from `GET /api/auth/me`, logout clears the HTTP-only cookie through `POST /api/auth/logout`, and each quick-preview drawer uses the existing matching detail endpoint instead of adding a shared DTO.

**Tech Stack:** ASP.NET Core controllers/auth cookies, xUnit endpoint tests, Next.js App Router, React client components, TypeScript API clients, Vitest + React Testing Library.

---

## File Structure

- Backend logout:
  - Modify `apps/api/Modules/Auth/Controllers/AuthController.cs`.
  - Test in `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs`.
- Frontend auth/header:
  - Modify `apps/front/src/lib/api/auth.ts`.
  - Modify `apps/front/src/components/auth/auth-provider.tsx`.
  - Modify `apps/front/src/components/layout/site-header.tsx`.
  - Test in `apps/front/src/components/layout/site-header.test.tsx`.
- Customer quick preview:
  - Create `apps/front/src/components/account/request-preview-drawer.tsx`.
  - Modify `apps/front/src/components/account/request-list.tsx`.
  - Modify `apps/front/src/app/account/requests/requests-page-client.tsx`.
  - Test in `apps/front/src/components/account/request-list.test.tsx` and `apps/front/src/app/account/requests/requests-page-client.test.tsx`.
- Admin quick preview:
  - Create `apps/front/src/components/admin/admin-request-preview-drawer.tsx`.
  - Modify `apps/front/src/components/admin/admin-request-list.tsx`.
  - Modify `apps/front/src/app/admin/requests/requests-page-client.tsx`.
  - Test in `apps/front/src/components/admin/admin-request-list.test.tsx` and `apps/front/src/app/admin/requests/requests-page-client.test.tsx`.
- Integration styling:
  - Modify `apps/front/src/app/globals.css` only after component work is integrated.

## Task 1: Backend Logout Endpoint

**Files:**
- Modify: `apps/api/Modules/Auth/Controllers/AuthController.cs`
- Test: `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs`

- [ ] **Step 1: Write failing endpoint tests**

Add tests proving logout clears the cookie and requires CSRF:

```csharp
[Fact]
public async Task Logout_WithCsrfToken_ReturnsNoContentAndClearsAuthCookie()
{
    var user = new CurrentUserDto(
        Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
        "Ivan Petrov",
        "ivan@example.com",
        "+79000000000",
        "customer");

    await using var factory = CreateFactory(new ReturningCustomerLoginService(user));
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    using var loginResponse = await client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequest("ivan@example.com", "secure-password"));
    var session = await ReadAuthSessionAsync(loginResponse);

    using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
    logoutRequest.Headers.Add(RequireCsrfTokenAttribute.HeaderName, session.CsrfToken);

    using var response = await client.SendAsync(logoutRequest);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));
    Assert.Contains(setCookieHeaders, header =>
        header.StartsWith("linecom_auth=", StringComparison.Ordinal) &&
        header.Contains("expires=", StringComparison.OrdinalIgnoreCase));
}

[Fact]
public async Task Logout_WithoutCsrfToken_ReturnsForbidden()
{
    var user = new CurrentUserDto(
        Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
        "Ivan Petrov",
        "ivan@example.com",
        "+79000000000",
        "customer");

    await using var factory = CreateFactory(new ReturningCustomerLoginService(user));
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    using var loginResponse = await client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequest("ivan@example.com", "secure-password"));
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    using var response = await client.PostAsync("/api/auth/logout", content: null);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var body = await ReadErrorAsync(response);
    Assert.Equal("auth.forbidden", body.Code);
}
```

- [ ] **Step 2: Run test to verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~AuthLoginEndpointTests" -m:1
```

Expected: logout tests fail because `/api/auth/logout` does not exist.

- [ ] **Step 3: Implement logout**

In `AuthController.cs`, add imports and endpoint:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
```

```csharp
[Authorize]
[RequireCsrfToken]
[HttpPost("logout")]
public async Task<IActionResult> Logout()
{
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return NoContent();
}
```

- [ ] **Step 4: Run test to verify GREEN**

Run the same filtered `dotnet test` command. Expected: all `AuthLoginEndpointTests` pass.

- [ ] **Step 5: Commit**

Commit only backend logout files:

```powershell
git add apps/api/Modules/Auth/Controllers/AuthController.cs tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs
git commit -m "feat: add csrf protected logout"
```

## Task 2: Auth Provider And Role-Aware Header

**Files:**
- Modify: `apps/front/src/lib/api/auth.ts`
- Modify: `apps/front/src/components/auth/auth-provider.tsx`
- Modify: `apps/front/src/components/layout/site-header.tsx`
- Test: `apps/front/src/components/layout/site-header.test.tsx`

- [ ] **Step 1: Write failing header tests**

Add tests for restored user, customer nav, admin nav, and logout:

```tsx
it("shows restored customer session and hides admin navigation", async () => {
  authApiMock.getMe.mockResolvedValue(customerSession);

  renderWithProviders(<SiteHeader />);

  expect(await screen.findByText("Иван Петров")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Мои заявки" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Профиль" })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Заявки клиентов" })).not.toBeInTheDocument();
});

it("shows admin navigation for seller session", async () => {
  authApiMock.getMe.mockResolvedValue(sellerSession);

  renderWithProviders(<SiteHeader />);

  expect(await screen.findByText("Мария Селлер")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Заявки клиентов" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Каталог" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Главная" })).toBeInTheDocument();
});

it("logs out with csrf token and returns to anonymous header", async () => {
  authApiMock.getMe.mockResolvedValue(customerSession);
  authApiMock.logout.mockResolvedValue(undefined);
  const user = userEvent.setup();

  renderWithProviders(<SiteHeader />);

  await screen.findByText("Иван Петров");
  await user.click(screen.getByRole("button", { name: "Выйти" }));

  expect(authApiMock.logout).toHaveBeenCalledWith(customerSession.csrfToken);
  expect(await screen.findByRole("link", { name: "Войти" })).toBeInTheDocument();
});
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
npm.cmd test -- src/components/layout/site-header.test.tsx
```

Expected: tests fail because session restore/logout/header role navigation are missing.

- [ ] **Step 3: Implement frontend auth API**

In `auth.ts`, add:

```ts
export function logout(csrfToken: string | null) {
  return apiJson<void>("/api/auth/logout", {
    method: "POST",
    csrfToken,
  });
}
```

- [ ] **Step 4: Implement session restore and logout state**

Extend `AuthProvider` with `isRestoringSession`, `restoreSession`, and `logoutSession`. `restoreSession` calls `getMe`, sets the session on success, and clears state on `auth.unauthorized`.

- [ ] **Step 5: Implement role-aware header**

Update `SiteHeader` to:

- call `restoreSession` on mount;
- show `Войти` only when `user === null`;
- show `user.name` and a `Выйти` button when authenticated;
- show `Профиль` and `Мои заявки` for authenticated users;
- show admin links only when `user.role === "seller" || user.role === "admin"`;
- call `logoutSession` from the `Выйти` button.

- [ ] **Step 6: Run tests to verify GREEN**

Run:

```powershell
npm.cmd test -- src/components/layout/site-header.test.tsx
```

Expected: header tests pass.

- [ ] **Step 7: Commit**

```powershell
git add apps/front/src/lib/api/auth.ts apps/front/src/components/auth/auth-provider.tsx apps/front/src/components/layout/site-header.tsx apps/front/src/components/layout/site-header.test.tsx
git commit -m "feat: show authenticated header navigation"
```

## Task 3: Customer Request Quick Preview

**Files:**
- Create: `apps/front/src/components/account/request-preview-drawer.tsx`
- Modify: `apps/front/src/components/account/request-list.tsx`
- Modify: `apps/front/src/app/account/requests/requests-page-client.tsx`
- Test: `apps/front/src/components/account/request-list.test.tsx`
- Test: `apps/front/src/app/account/requests/requests-page-client.test.tsx`

- [ ] **Step 1: Write failing component tests**

Add tests that the list exposes a quick-preview button and the page loads customer detail with `getCustomerRequest`.

Expected behavior:

- `RequestList` renders `Быстрый просмотр ЗК26-0001`;
- clicking it calls `onPreviewRequest("ЗК26-0001")`;
- page opens a drawer showing loaded customer detail;
- drawer does not render `internalComment`.

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
npm.cmd test -- src/components/account/request-list.test.tsx src/app/account/requests/requests-page-client.test.tsx
```

Expected: tests fail because preview props/drawer do not exist.

- [ ] **Step 3: Implement drawer component**

Create `RequestPreviewDrawer` with props:

```ts
type RequestPreviewDrawerProps = {
  request: CustomerRequestDetail | null;
  isOpen: boolean;
  isLoading: boolean;
  error: string | null;
  onClose: () => void;
};
```

Render title, status, date, comment, items, history, close button, and `Открыть полностью`.

- [ ] **Step 4: Wire list and page**

Add `onPreviewRequest` to `RequestList`. In `RequestsPageClient`, keep `previewNumber`, `previewRequest`, `isPreviewLoading`, `previewError`; load detail through `getCustomerRequest(number)`.

- [ ] **Step 5: Run tests to verify GREEN**

Run the same targeted `npm.cmd test` command. Expected: customer preview tests pass.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/components/account/request-preview-drawer.tsx apps/front/src/components/account/request-list.tsx apps/front/src/app/account/requests/requests-page-client.tsx apps/front/src/components/account/request-list.test.tsx apps/front/src/app/account/requests/requests-page-client.test.tsx
git commit -m "feat: add customer request quick preview"
```

## Task 4: Admin Request Quick Preview

**Files:**
- Create: `apps/front/src/components/admin/admin-request-preview-drawer.tsx`
- Modify: `apps/front/src/components/admin/admin-request-list.tsx`
- Modify: `apps/front/src/app/admin/requests/requests-page-client.tsx`
- Test: `apps/front/src/components/admin/admin-request-list.test.tsx`
- Test: `apps/front/src/app/admin/requests/requests-page-client.test.tsx`

- [ ] **Step 1: Write failing component tests**

Add tests that the admin list exposes a quick-preview button and the page loads admin detail through `getAdminRequest`.

Expected behavior:

- `AdminRequestList` renders `Быстрый просмотр ЗК26-0001`;
- clicking it calls `onPreviewRequest("ЗК26-0001")`;
- drawer renders customer snapshot, organization snapshot, internal comment, items, history;
- drawer links to `/admin/requests/[number]` with text `Открыть обработку`;
- drawer does not render status/comment editing controls.

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
npm.cmd test -- src/components/admin/admin-request-list.test.tsx src/app/admin/requests/requests-page-client.test.tsx
```

Expected: tests fail because admin preview props/drawer do not exist.

- [ ] **Step 3: Implement admin drawer component**

Create `AdminRequestPreviewDrawer` with props:

```ts
type AdminRequestPreviewDrawerProps = {
  request: AdminRequestDetail | null;
  isOpen: boolean;
  isLoading: boolean;
  error: string | null;
  onClose: () => void;
};
```

Render admin-only fields and full processing link. Do not add mutation controls.

- [ ] **Step 4: Wire list and page**

Add `onPreviewRequest` to `AdminRequestList`. In admin `RequestsPageClient`, keep preview state and load detail through `getAdminRequest(number)`.

- [ ] **Step 5: Run tests to verify GREEN**

Run the same targeted `npm.cmd test` command. Expected: admin preview tests pass.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/components/admin/admin-request-preview-drawer.tsx apps/front/src/components/admin/admin-request-list.tsx apps/front/src/app/admin/requests/requests-page-client.tsx apps/front/src/components/admin/admin-request-list.test.tsx apps/front/src/app/admin/requests/requests-page-client.test.tsx
git commit -m "feat: add admin request quick preview"
```

## Task 5: Styling And Final Integration

**Files:**
- Modify: `apps/front/src/app/globals.css`
- Potentially adjust tests touched by previous tasks.

- [ ] **Step 1: Add drawer/header styling**

Add responsive styles for:

- authenticated header user cluster;
- logout button state;
- account/admin navigation grouping;
- preview drawer overlay/panel;
- drawer item/history sections.

- [ ] **Step 2: Run frontend checks**

Run:

```powershell
npm.cmd test
npm.cmd run lint
npm.cmd run build
```

Expected: all pass.

- [ ] **Step 3: Run backend checks**

Run:

```powershell
dotnet test LineCom.sln -m:1
```

Expected: all pass. NuGet vulnerability feed warnings are acceptable if tests pass.

- [ ] **Step 4: Browser QA**

Start frontend/backend as usual for this repo and verify:

- anonymous header;
- authenticated customer header;
- seller/admin header;
- logout;
- `/account/requests` quick preview desktop/mobile;
- `/admin/requests` quick preview desktop/mobile;
- no horizontal overflow and no blank pages.

- [ ] **Step 5: Commit**

```powershell
git add apps/front/src/app/globals.css
git commit -m "style: polish request quick preview"
```

## Self-Review

- Spec coverage: logout, auth restore, role-aware navigation, customer preview, admin preview, data separation, tests, and SEO/GEO non-impact are covered.
- Placeholder scan: no `TODO`, `TBD`, or intentionally vague implementation steps remain.
- Type consistency: plan uses existing `CustomerRequestDetail`, `AdminRequestDetail`, `getCustomerRequest`, and `getAdminRequest` names from the current API clients.

## Handoff After Browser Review

Status: reviewed by the user in Playwright on 2026-05-13.

What was reviewed:

- feature worktree: `D:\Projects\FL\LineCom\.worktrees\account-request-quick-view`;
- branch: `feature/account-request-quick-view`;
- local frontend: `http://127.0.0.1:3000`;
- local API: `http://127.0.0.1:8080`;
- API was restarted for review with the existing local server database connection from the non-committed local config. The secret value was not printed and was not written into repository files.

Observed blocker and resolution during review:

- initial Playwright login failed because local PostgreSQL on `localhost:5432` was unavailable;
- local API was then pointed at the deployed server database for review;
- `GET /api/public/system/health` and `GET /api/public/catalog/categories` returned `200`;
- the remaining `401` from `/api/auth/me` before login is expected for an anonymous browser session.

Continuation note:

- The user confirmed that the current result was reviewed.
- Further product discussion is intentionally deferred until after context cleanup.
- On resume, do not restart analysis from scratch: continue from this branch and this handoff note.
