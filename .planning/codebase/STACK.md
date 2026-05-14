# Technology Stack

**Analysis Date:** 2026-05-14

## Languages

**Primary:**
- C# / .NET 8 - backend API, DbUp migrator, catalog importer, and API tests in `apps/api/LineCom.Api.csproj`, `apps/dbmigrator/LineCom.DbMigrator.csproj`, `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`, `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`, and `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.
- TypeScript - Next.js frontend source and tests in `apps/front/src`, configured by `apps/front/tsconfig.json`.

**Secondary:**
- SQL - PostgreSQL schema/data migrations stored as embedded DbUp scripts in `apps/dbmigrator/Migrations/*.sql`.
- JSON - application settings and catalog import inputs/manifests are used by `apps/api/appsettings.json`, `apps/catalog-import.core/Source/OneCExportReader.cs`, and `apps/catalog-import.core/Images/ProductImageManifestReader.cs`.
- Markdown - product and architecture source-of-truth documentation lives under `vault/Человекочитаемое`.

## Runtime

**Environment:**
- .NET SDK `8.0.418` with `rollForward: latestFeature` from `global.json`.
- Backend target framework is `net8.0` in `apps/api/LineCom.Api.csproj`.
- DbUp migrator target framework is `net8.0` in `apps/dbmigrator/LineCom.DbMigrator.csproj`.
- Catalog import core target framework is `net8.0` in `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`.
- Catalog import desktop UI target framework is `net8.0-windows` with Windows Forms in `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`.
- Frontend runs on Node.js in production according to `vault/Человекочитаемое/Production deployment line-com.ru.md`; the exact Node version is not pinned in repo files.

**Package Manager:**
- Frontend uses npm; lockfile `apps/front/package-lock.json` is present with `lockfileVersion: 3`.
- .NET packages are managed through SDK-style `.csproj` files under `apps` and `tests`.

## Frameworks

**Core:**
- ASP.NET Core Web API on .NET 8 - controller-based backend entry point in `apps/api/Program.cs` and `apps/api/LineCom.Api.csproj`.
- Next.js `16.2.4` App Router - frontend application in `apps/front/package.json` and `apps/front/next.config.ts`.
- React `19.2.4` and React DOM `19.2.4` - UI runtime in `apps/front/package.json`.
- Windows Forms - catalog import desktop tool in `apps/catalog-import.winforms/MainForm.cs` and `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`.

**Testing:**
- xUnit `2.5.3` - backend and importer tests in `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.
- Microsoft.AspNetCore.Mvc.Testing `8.0.26` - API integration test host in `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.
- Vitest `^4.1.5` with jsdom `^29.1.1` - frontend tests configured by `apps/front/vitest.config.ts`.
- Testing Library packages - React component tests via `@testing-library/react`, `@testing-library/jest-dom`, and `@testing-library/user-event` in `apps/front/package.json`.

**Build/Dev:**
- Next.js build uses `next build` and standalone output configured in `apps/front/package.json` and `apps/front/next.config.ts`.
- ESLint `^9` with `eslint-config-next` `16.2.4` - frontend lint config in `apps/front/eslint.config.mjs`.
- TypeScript `^5` - frontend compilation settings in `apps/front/tsconfig.json`.
- Swashbuckle.AspNetCore `6.4.0` - Swagger/OpenAPI development UI registered in `apps/api/Program.cs`.
- DbUp PostgreSQL `7.0.1` - SQL migration runner in `apps/dbmigrator/LineCom.DbMigrator.csproj` and `apps/dbmigrator/Program.cs`.

## Key Dependencies

**Critical:**
- `Npgsql` `10.0.2` - backend PostgreSQL driver in `apps/api/LineCom.Api.csproj`; connection factory uses `NpgsqlDataSource` in `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`.
- `Dapper` `2.1.72` - backend SQL mapping in `apps/api/LineCom.Api.csproj`; repositories and queries use Dapper under `apps/api/Modules`.
- `dbup-postgresql` `7.0.1` - migration execution in `apps/dbmigrator/LineCom.DbMigrator.csproj`.
- `next` `16.2.4` - frontend framework in `apps/front/package.json`.
- `react` / `react-dom` `19.2.4` - frontend rendering in `apps/front/package.json`.

**Infrastructure:**
- `Swashbuckle.AspNetCore` `6.4.0` - development Swagger endpoints enabled only for development in `apps/api/Program.cs`.
- `Microsoft.AspNetCore.Authentication.Cookies` from ASP.NET Core - custom HttpOnly cookie session setup in `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`.
- `System.Security.Cryptography` - password hashing and CSRF token generation in `apps/api/Modules/Auth/Services/Pbkdf2PasswordHasher.cs` and `apps/api/Modules/Auth/Services/CookieAuthSessionService.cs`.
- `Microsoft.Extensions.FileProviders.PhysicalFileProvider` - local storage static file serving at `/storage` in `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`.
- `postcss` override `8.5.10` - dependency override in `apps/front/package.json`, documented in `vault/Человекочитаемое/Технический стек.md`.

## Configuration

**Environment:**
- Backend configuration loads `apps/api/appsettings.json` plus optional `appsettings.Local.json` in `apps/api/Program.cs`.
- Backend database connection key is `ConnectionStrings:Default`, read by `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`.
- DbUp migrator reads the connection string from the first CLI argument or `LINECOM_CONNECTION_STRING` in `apps/dbmigrator.core/MigrationConfiguration.cs`.
- Frontend server-side API origin is `LINECOM_API_ORIGIN`, defaulting to `http://127.0.0.1:8080`, in `apps/front/next.config.ts` and `apps/front/src/lib/api/http.ts`.
- Local storage root is `Storage:RootPath`, with fallback to `apps/api/storage` relative to the API content root, in `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs` and `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`.
- `.env.local` and `.env.example` exist under `apps/front`; contents were not read because env files can contain secrets.
- `apps/api/appsettings.Local.json` exists and is ignored by `.gitignore`; contents were not read as local environment configuration.

**Build:**
- Solution file: `LineCom.sln`.
- .NET SDK pin: `global.json`.
- Backend project: `apps/api/LineCom.Api.csproj`.
- Db migrator project: `apps/dbmigrator/LineCom.DbMigrator.csproj`.
- Catalog importer projects: `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj` and `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`.
- Frontend package/config files: `apps/front/package.json`, `apps/front/package-lock.json`, `apps/front/next.config.ts`, `apps/front/tsconfig.json`, `apps/front/eslint.config.mjs`, and `apps/front/vitest.config.ts`.

## Platform Requirements

**Development:**
- .NET 8 SDK compatible with `global.json`.
- npm/Node.js for `apps/front`; exact Node version is not pinned in repo.
- PostgreSQL database reachable by `ConnectionStrings:Default` for API and by CLI arg or `LINECOM_CONNECTION_STRING` for migrations.
- Windows is required for the WinForms UI project `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`.

**Production:**
- Ubuntu 24.04 LTS, nginx, ASP.NET Core Runtime 8, Node.js, PostgreSQL 16, and Let's Encrypt/certbot are documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`.
- Deployment layout documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`: API under `/opt/linecom/api/current`, frontend under `/opt/linecom/front/current`, dbmigrator under `/opt/linecom/dbmigrator/current`, local storage under `/var/lib/linecom/storage`, API env under `/etc/linecom/api.env`, and frontend env under `/etc/linecom/front.env`.
- Systemd services documented in `vault/Человекочитаемое/Production deployment line-com.ru.md`: `linecom-api.service` listens on `127.0.0.1:8080`, `linecom-front.service` listens on `127.0.0.1:3000`.
- nginx proxies `/` to Next.js, `/api/` to ASP.NET Core, and `/storage/` to the API storage endpoint according to `vault/Человекочитаемое/Production deployment line-com.ru.md`.

## Source-of-Truth Constraints

- Backend uses PostgreSQL through Npgsql and Dapper; Entity Framework is not used. This is confirmed by `vault/Человекочитаемое/Архитектура backend и БД.md`, `apps/api/LineCom.Api.csproj`, and Dapper/Npgsql repositories under `apps/api/Modules`.
- Database migrations are SQL scripts executed through the DbUp migrator. This is confirmed by `apps/dbmigrator/Program.cs`, `apps/dbmigrator/LineCom.DbMigrator.csproj`, and `apps/dbmigrator/Migrations/*.sql`.
- Local FileStorage is the target file-storage approach, not a temporary S3/MinIO substitute. This is documented in `vault/Человекочитаемое/Архитектура backend и БД.md` and implemented in `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs` and `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`.
- SEO/GEO is a cross-cutting requirement for public catalog, routing, metadata, sitemap, canonical URLs, and server rendering. This is documented in `vault/Человекочитаемое/Сквозные требования.md`, `vault/Человекочитаемое/Технический стек.md`, and implemented through Next.js App Router files under `apps/front/src/app`.

---

*Stack analysis: 2026-05-14*
