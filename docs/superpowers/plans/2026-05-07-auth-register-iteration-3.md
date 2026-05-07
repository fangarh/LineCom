# Auth Register Iteration 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement customer registration through `POST /api/auth/register`.

**Architecture:** Add an `Auth` module beside `Catalog`. The controller delegates validation and persistence to a registration service, which uses a focused Dapper repository and a PBKDF2 password hasher. Successful registration signs in through ASP.NET Core cookie authentication and returns the contract `AuthSessionDto` with a CSRF token stored as an encrypted auth claim.

**Tech Stack:** .NET 8, ASP.NET Core controllers and cookie authentication, Dapper, Npgsql, xUnit, WebApplicationFactory.

---

### Task 1: Register Service RED Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Modules/Auth/CustomerRegistrationServiceTests.cs`

- [x] **Step 1: Write failing tests**

Cover successful customer creation, required contact validation, password length validation, duplicate-contact mapping to `auth.user_already_exists`, and plaintext password avoidance.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter CustomerRegistrationServiceTests
```

Expected: fail because the Auth module types do not exist.

### Task 2: Register Endpoint RED Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Modules/Auth/AuthRegisterEndpointTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Auth/AuthModuleRegistrationTests.cs`

- [x] **Step 1: Write failing endpoint and DI tests**

Cover `201 Created`, camelCase response, `linecom_auth` cookie, invalid-contact error mapping, duplicate-contact error mapping, and Auth service registrations.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "AuthRegisterEndpointTests|AuthModuleRegistrationTests"
```

Expected: fail because Auth module registration, controller, DTOs and services do not exist.

### Task 3: Implement Auth Module

**Files:**
- Modify: `apps/api/Program.cs`
- Create: `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`
- Create: `apps/api/Modules/Auth/Controllers/AuthController.cs`
- Create: `apps/api/Modules/Auth/DTOs/AuthDtos.cs`
- Create: `apps/api/Modules/Auth/Services/AuthErrors.cs`
- Create: `apps/api/Modules/Auth/Services/CustomerRegistrationService.cs`
- Create: `apps/api/Modules/Auth/Services/IAuthSessionService.cs`
- Create: `apps/api/Modules/Auth/Services/ICustomerRegistrationService.cs`
- Create: `apps/api/Modules/Auth/Services/IPasswordHasher.cs`
- Create: `apps/api/Modules/Auth/Services/Pbkdf2PasswordHasher.cs`
- Create: `apps/api/Modules/Auth/Services/CookieAuthSessionService.cs`
- Create: `apps/api/Modules/Auth/Repositories/IUserRegistrationRepository.cs`
- Create: `apps/api/Modules/Auth/Repositories/DapperUserRegistrationRepository.cs`

- [x] **Step 1: Add DTOs and service interfaces**

Define the public JSON contract and internal service boundaries.

- [x] **Step 2: Add validation, normalization and password hashing**

Normalize email with trim/lowercase, normalize phone to a compact `+`/digit shape, validate password length 8..128, hash through PBKDF2-SHA256 with random salt and no plaintext storage.

- [x] **Step 3: Add Dapper insert repository**

Insert `name`, normalized contacts, `password_hash`, role `customer`, and active status. Map PostgreSQL unique violations on email/phone to duplicate-contact errors.

- [x] **Step 4: Add cookie session service and controller**

Use `SignInAsync` with `CookieAuthenticationDefaults.AuthenticationScheme`, claims for user id/name/email/phone/role/CSRF, cookie name `linecom_auth`, `HttpOnly`, `SameSite=Lax`, and production `Secure`.

- [x] **Step 5: Register module in DI and middleware**

Call `AddAuthModule`, `UseAuthentication`, then `UseAuthorization`.

- [x] **Step 6: Run targeted tests and verify GREEN**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "CustomerRegistrationServiceTests|AuthRegisterEndpointTests|AuthModuleRegistrationTests"
```

Expected: pass.

### Task 4: Verify and Close Iteration

**Files:**
- Modify: `vault/Человекочитаемое/Auth Request Core iterations.md`
- Modify: `C:\Users\Fangarh\.codex\memories\linecom.md` if the memory file exists and needs the continuation point updated.

- [x] **Step 1: Run solution build**

Run:

```powershell
dotnet build LineCom.sln
```

Expected: build succeeds.

- [x] **Step 2: Run solution tests**

Run:

```powershell
dotnet test LineCom.sln
```

Expected: tests pass.

- [x] **Step 3: Update Obsidian status**

Mark iteration 3 complete and set the recommended next continuation point to iteration 4.

- [x] **Step 4: Check debt markers**

Search changed project files for `TODO`, `TBD`, `заглуш`, `костыл` and resolve accidental markers before finishing.
