# External Integrations

**Analysis Date:** 2026-05-14

## APIs & External Services

**Frontend-to-Backend API:**
- Next.js calls the ASP.NET Core API through `/api/...` routes and includes cookies on requests.
  - Client: `fetch` wrapper in `apps/front/src/lib/api/http.ts`.
  - Routing/proxy: rewrites `/api/:path*` to `LINECOM_API_ORIGIN` in `apps/front/next.config.ts`.
  - Auth transport: `credentials: "include"` in `apps/front/src/lib/api/http.ts`.
  - CSRF header: `X-CSRF-Token` in `apps/front/src/lib/api/http.ts`.

**Public HTTP API:**
- Public catalog and system endpoints are exposed under `/api/public/...`.
  - Controllers: `apps/api/Modules/Catalog/Controllers/PublicCategoriesController.cs`, `apps/api/Modules/Catalog/Controllers/PublicProductsController.cs`, `apps/api/Modules/Catalog/Controllers/PublicHomepageController.cs`, and `apps/api/Modules/System/Controllers/HealthController.cs`.
  - API grouping is documented in `vault/Человекочитаемое/Архитектура backend и БД.md`.

**Account/Admin HTTP API:**
- Authenticated account endpoints live under `/api/account/...`; admin endpoints live under `/api/admin/...`.
  - Account controllers: `apps/api/Modules/Account/Controllers/AccountProfileController.cs` and `apps/api/Modules/Requests/Controllers/CustomerRequestsController.cs`.
  - Admin controllers: `apps/api/Modules/Requests/Controllers/AdminRequestsController.cs`, `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`, `apps/api/Modules/Catalog/Controllers/AdminCatalogProductImagesController.cs`, `apps/api/Modules/Catalog/Controllers/AdminCatalogCategoriesController.cs`, `apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs`, `apps/api/Modules/Catalog/Controllers/AdminCatalogAttributesController.cs`, and `apps/api/Modules/Catalog/Controllers/AdminHomepageController.cs`.

**Swagger/OpenAPI:**
- Swagger is registered for development only.
  - SDK/Client: `Swashbuckle.AspNetCore` in `apps/api/LineCom.Api.csproj`.
  - Implementation: `AddSwaggerGen`, `UseSwagger`, and `UseSwaggerUI` in `apps/api/Program.cs`.
  - Auth: local development API surface, not a production integration.

**Domain, DNS, TLS, and Reverse Proxy:**
- Production domain is `line-com.ru` with `www.line-com.ru`.
  - DNS provider: RU-CENTER DNS-master documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - Web server: nginx documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - TLS: Let's Encrypt via certbot documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - API reverse-proxy header handling: `apps/api/Infrastructure/Hosting/ReverseProxyForwardingPolicy.cs`.
  - HTTPS redirect policy: `apps/api/Infrastructure/Hosting/HttpsRedirectionPolicy.cs`.

**Mail Infrastructure:**
- Mail remains on Nicmail/RU-CENTER DNS records; the application code does not send email.
  - DNS/mail configuration: `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - App SMTP client: not detected in `apps/api`, `apps/front`, `apps/catalog-import.core`, or `apps/catalog-import.winforms`.

**1C Catalog Export Files:**
- Catalog importer reads a local 1C export JSON and optional image manifest JSON.
  - Reader: `apps/catalog-import.core/Source/OneCExportReader.cs`.
  - Image manifest reader: `apps/catalog-import.core/Images/ProductImageManifestReader.cs`.
  - UI defaults: `apps/catalog-import.winforms/MainForm.cs`.
  - External network API: not detected; inputs are local files under `Assets` by convention.

## Data Storage

**Databases:**
- PostgreSQL is the only application database.
  - Connection: `ConnectionStrings:Default` for API in `apps/api/appsettings.json` and `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`.
  - Migrator connection: CLI arg or `LINECOM_CONNECTION_STRING` in `apps/dbmigrator.core/MigrationConfiguration.cs`.
  - Client: `NpgsqlDataSource` in `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`.
  - Query mapper: Dapper repositories and queries under `apps/api/Modules`.
  - Production version: PostgreSQL 16 documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.

**Migrations:**
- DbUp executes embedded SQL migration scripts.
  - Runner: `apps/dbmigrator/Program.cs`.
  - Package: `dbup-postgresql` in `apps/dbmigrator/LineCom.DbMigrator.csproj`.
  - Scripts: `apps/dbmigrator/Migrations/001_extensions.sql` through `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql`.
  - Journal table: `public.schema_versions` in `apps/dbmigrator/Program.cs`.

**File Storage:**
- Local FileStorage is the target approach and stores product/brand/catalog files on disk.
  - API storage root config: `Storage:RootPath` in `apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs`.
  - API writer: `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`.
  - Static serving: `/storage` via `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`.
  - Frontend rewrite: `/storage/:path*` to `LINECOM_API_ORIGIN` in `apps/front/next.config.ts`.
  - Production root: `/var/lib/linecom/storage` documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - Storage entity integration: `stored_files` and `product_images` usage in `apps/catalog-import.core/Database/CatalogImportDatabase.cs`.
  - S3/MinIO target storage: explicitly not used per `vault/Человекочитаемое/Архитектура backend и БД.md`.

**Caching:**
- External cache service not detected.
  - No Redis/Memcached/Hangfire/cache package detected in `apps/api/*.csproj`, `apps/front/package.json`, or integration searches.
  - Next.js request caching can be passed through `apps/front/src/lib/api/http.ts`, but no external cache provider is configured.

## Authentication & Identity

**Auth Provider:**
- Custom ASP.NET Core cookie authentication.
  - Implementation: `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`.
  - Cookie name: `linecom_auth` in `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`.
  - Cookie security: HttpOnly, SameSite Lax, production Secure policy in `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`.
  - Session creation: claims-based sign-in in `apps/api/Modules/Auth/Services/CookieAuthSessionService.cs`.
  - Password hashing: PBKDF2 service in `apps/api/Modules/Auth/Services/Pbkdf2PasswordHasher.cs`.
  - User storage: PostgreSQL users accessed through Dapper repositories in `apps/api/Modules/Auth/Repositories/DapperUserRegistrationRepository.cs` and `apps/api/Modules/Auth/Repositories/DapperUserLoginRepository.cs`.

**CSRF:**
- Authenticated mutating endpoints require `X-CSRF-Token`.
  - Server enforcement: `apps/api/Modules/Auth/Services/RequireCsrfTokenAttribute.cs`.
  - Token source: auth claim generated in `apps/api/Modules/Auth/Services/CookieAuthSessionService.cs`.
  - Frontend transport: `apps/front/src/lib/api/http.ts`.
  - Protected endpoints: `[RequireCsrfToken]` across account/admin/request/catalog controllers under `apps/api/Modules`.

**Roles and Authorization:**
- Roles are `customer`, `seller`, and `admin` per `vault/Человекочитаемое/Архитектура backend и БД.md`.
  - Controller gate: `[Authorize]` on account/admin controllers under `apps/api/Modules`.
  - Staff checks: catalog and admin services under `apps/api/Modules/Catalog/Services` and request services under `apps/api/Modules/Requests/Services`.

**External Identity Providers:**
- OAuth/OIDC/social login provider not detected.
  - No IdentityServer/Auth0/Azure AD/Google OAuth package detected in `apps/api/LineCom.Api.csproj` or `apps/front/package.json`.

## Monitoring & Observability

**Error Tracking:**
- External error tracking service not detected.
  - No Sentry/Application Insights/OpenTelemetry package detected in `apps/api/LineCom.Api.csproj` or `apps/front/package.json`.

**Logs:**
- ASP.NET Core logging is configured through standard providers.
  - Development console/debug logging policy: `apps/api/Infrastructure/Hosting/DevelopmentLoggingPolicy.cs`.
  - Logging config keys: `apps/api/appsettings.json`.
  - Unhandled API exceptions are normalized by `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`.
- DbUp logs to console in `apps/dbmigrator/Program.cs`.
- Catalog importer logs to the WinForms UI text box in `apps/catalog-import.winforms/MainForm.cs`.

## CI/CD & Deployment

**Hosting:**
- Production hosting is a single Ubuntu server with nginx reverse proxy, ASP.NET Core Runtime 8, Node.js, PostgreSQL 16, and systemd.
  - Source: `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - API service: `linecom-api.service` documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.
  - Frontend service: `linecom-front.service` documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.

**CI Pipeline:**
- Repository CI pipeline not detected.
  - No `.github`, `.gitlab-ci.yml`, `azure-pipelines.yml`, `Jenkinsfile`, `Dockerfile`, or `docker-compose*.yml` file detected in the repo scan.

**Deployment Artifacts:**
- Frontend produces Next.js standalone output through `output: "standalone"` in `apps/front/next.config.ts`.
- Backend and dbmigrator are standard .NET publish outputs from `apps/api/LineCom.Api.csproj` and `apps/dbmigrator/LineCom.DbMigrator.csproj`.
- Production release directories are documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.

## Environment Configuration

**Required env vars and config keys:**
- `ConnectionStrings:Default` - API PostgreSQL connection in `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`.
- `LINECOM_CONNECTION_STRING` - DbUp migrator fallback connection string in `apps/dbmigrator.core/MigrationConfiguration.cs`.
- `LINECOM_API_ORIGIN` - frontend server-side API target in `apps/front/next.config.ts` and `apps/front/src/lib/api/http.ts`.
- `Storage:RootPath` - optional API local storage root in `apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs`.
- `LC_SSH_HOST`, `LC_SSH_USER`, `LC_SSH_PASSWORD` - deployment automation variables documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.

**Secrets location:**
- `apps/front/.env.local` exists and is ignored by `.gitignore`; contents were not read.
- `apps/front/.env.example` exists as a template; contents were not read due env-file safety policy.
- `apps/api/appsettings.Local.json` exists and is ignored by `.gitignore`; contents were not read.
- `.codex-local/linecom-ssh.env` exists and is ignored by `.gitignore`; contents were not read.
- `.codex-local/site-admin-credentials.txt` exists and is ignored by `.gitignore`; contents were not read.
- Production API/frontend env files are documented as `/etc/linecom/api.env` and `/etc/linecom/front.env` in `vault/Человекочитаемое/Production deployment line-com.ru.md`.

## Webhooks & Callbacks

**Incoming:**
- Incoming third-party webhook endpoints not detected.
  - API controllers under `apps/api/Modules` expose public/account/admin/auth routes, but no controller or route naming indicates webhook callbacks.

**Outgoing:**
- Outgoing third-party webhook calls not detected.
  - No payment, notification, queue, or webhook client package detected in `apps/api/LineCom.Api.csproj`, `apps/front/package.json`, or integration searches.

**Payments and Notifications:**
- Online payments are outside the product model; no Stripe/YooKassa/payment SDK is detected.
  - Product model note: `vault/Человекочитаемое/Архитектура backend и БД.md`.
  - Package scan: `apps/api/LineCom.Api.csproj` and `apps/front/package.json`.
- Email/SMS/Telegram application notifications are not implemented.
  - No SMTP/SMS/Telegram package or client code detected in `apps/api`, `apps/front`, or `apps/catalog-import.core`.

---

*Integration audit: 2026-05-14*
