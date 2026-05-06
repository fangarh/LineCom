# Backend Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Подготовить релизный backend foundation для LineCom: модульный ASP.NET Core API без Entity Framework, с Npgsql, Dapper, DbUp SQL migrations, тестовой инфраструктурой, health endpoint и единым контуром ошибок.

**Architecture:** Backend остается модульным монолитом в `apps/api`. Доступ к PostgreSQL идет через `NpgsqlDataSource`, SQL-запросы маппятся через Dapper, миграции выполняются отдельным DbUp console runner. Foundation не реализует доменные таблицы каталога и заявок, но задает строгую структуру, в которую они будут добавлены следующими планами.

**Tech Stack:** .NET 8, ASP.NET Core Web API controllers, Npgsql, Dapper, DbUp PostgreSQL, xUnit, Microsoft.AspNetCore.Mvc.Testing, PostgreSQL SQL migrations.

---

## Scope

Входит в план:

- тестовый проект для backend;
- модульная структура папок `Modules`, `Infrastructure`, `Shared`;
- удаление шаблонного WeatherForecast;
- публичный health endpoint;
- регистрация `NpgsqlDataSource`;
- `DbConnectionFactory`;
- отдельный DbUp migration runner;
- первая SQL-миграция для PostgreSQL extensions;
- единый JSON-формат ошибок;
- документация по запуску foundation.

Не входит в план:

- auth/cookie/CSRF;
- доменные таблицы каталога;
- характеристики товаров;
- заявки;
- Excel-импорт;
- frontend-интеграция.

## File Map

Создать:

- `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj` - тестовый проект.
- `tests/LineCom.Api.Tests/System/HealthEndpointTests.cs` - интеграционный тест health endpoint.
- `tests/LineCom.Api.Tests/Infrastructure/Database/DatabaseRegistrationTests.cs` - тест регистрации БД.
- `tests/LineCom.Api.Tests/Shared/Errors/ApiExceptionMiddlewareTests.cs` - тест формата ошибок.
- `apps/api/Modules/System/Controllers/HealthController.cs` - публичный health endpoint.
- `apps/api/Infrastructure/Database/IDbConnectionFactory.cs` - интерфейс открытия соединений.
- `apps/api/Infrastructure/Database/NpgsqlConnectionFactory.cs` - реализация через `NpgsqlDataSource`.
- `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs` - регистрация Npgsql.
- `apps/api/Shared/Errors/ApiErrorResponse.cs` - единый ответ ошибки.
- `apps/api/Shared/Errors/ApiException.cs` - контролируемая API-ошибка.
- `apps/api/Shared/Errors/ApiExceptionMiddleware.cs` - middleware исключений.
- `apps/dbmigrator/LineCom.DbMigrator.csproj` - console runner миграций.
- `apps/dbmigrator/Program.cs` - запуск DbUp.
- `apps/dbmigrator/Migrations/001_extensions.sql` - первая SQL-миграция.
- `vault/Человекочитаемое/Backend foundation.md` - человекочитаемая инструкция запуска.

Изменить:

- `LineCom.sln` - добавить проекты API tests и DB migrator.
- `apps/api/LineCom.Api.csproj` - добавить Npgsql и Dapper.
- `apps/api/Program.cs` - подключить controllers, database services, middleware, health controller, partial `Program`.
- `apps/api/appsettings.json` - добавить `ConnectionStrings.Default`.
- `apps/api/appsettings.Development.json` - добавить dev connection string.

Удалить:

- `apps/api/WeatherForecast.cs`
- `apps/api/Controllers/WeatherForecastController.cs`

## Task 1: Создать тестовый проект backend

**Files:**

- Create: `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`
- Modify: `LineCom.sln`

- [ ] **Step 1: Создать xUnit test project**

Run:

```powershell
dotnet new xunit -n LineCom.Api.Tests -o tests/LineCom.Api.Tests
```

Expected: создан файл `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.

- [ ] **Step 2: Добавить project reference на API**

Run:

```powershell
dotnet add tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj reference apps/api/LineCom.Api.csproj
```

Expected: в test `.csproj` появился `ProjectReference` на `apps/api/LineCom.Api.csproj`.

- [ ] **Step 3: Добавить package для интеграционного тестирования ASP.NET Core**

Run:

```powershell
dotnet add tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.26
```

Expected: package `Microsoft.AspNetCore.Mvc.Testing` версии `8.0.26` добавлен в test `.csproj`.

- [ ] **Step 4: Добавить тестовый проект в solution**

Run:

```powershell
dotnet sln LineCom.sln add tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj
```

Expected: `LineCom.sln` содержит `LineCom.Api.Tests`.

- [ ] **Step 5: Запустить тесты**

Run:

```powershell
dotnet test LineCom.sln
```

Expected: PASS. На этом шаге тестовый проект может содержать только шаблонный `UnitTest1`.

- [ ] **Step 6: Удалить шаблонный тест**

Delete:

```text
tests/LineCom.Api.Tests/UnitTest1.cs
```

Expected: файл удален.

- [ ] **Step 7: Checkpoint**

Если в рабочей папке уже есть `.git`, выполнить:

```powershell
git add LineCom.sln tests/LineCom.Api.Tests
git commit -m "test: add api test project"
```

Если `.git` отсутствует, зафиксировать в ответе исполнителя список измененных файлов.

## Task 2: Подготовить package references backend

**Files:**

- Modify: `apps/api/LineCom.Api.csproj`

- [ ] **Step 1: Добавить Npgsql**

Run:

```powershell
dotnet add apps/api/LineCom.Api.csproj package Npgsql --version 10.0.2
```

Expected: `apps/api/LineCom.Api.csproj` содержит `PackageReference Include="Npgsql" Version="10.0.2"`.

- [ ] **Step 2: Добавить Dapper**

Run:

```powershell
dotnet add apps/api/LineCom.Api.csproj package Dapper --version 2.1.72
```

Expected: `apps/api/LineCom.Api.csproj` содержит `PackageReference Include="Dapper" Version="2.1.72"`.

- [ ] **Step 3: Проверить restore**

Run:

```powershell
dotnet restore LineCom.sln
```

Expected: restore завершен без ошибок.

- [ ] **Step 4: Запустить build**

Run:

```powershell
dotnet build LineCom.sln
```

Expected: build завершен без ошибок.

- [ ] **Step 5: Checkpoint**

Если `.git` есть:

```powershell
git add apps/api/LineCom.Api.csproj tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj
git commit -m "chore: add backend data access packages"
```

Если `.git` отсутствует, не коммитить и перечислить измененные `.csproj` файлы в отчете.

## Task 3: Удалить шаблонный WeatherForecast

**Files:**

- Delete: `apps/api/WeatherForecast.cs`
- Delete: `apps/api/Controllers/WeatherForecastController.cs`

- [ ] **Step 1: Удалить шаблонные файлы**

Delete:

```text
apps/api/WeatherForecast.cs
apps/api/Controllers/WeatherForecastController.cs
```

Expected: шаблонный endpoint больше не компилируется и не доступен.

- [ ] **Step 2: Запустить build**

Run:

```powershell
dotnet build LineCom.sln
```

Expected: PASS. Если build падает из-за ссылок на WeatherForecast, удалить эти ссылки.

- [ ] **Step 3: Checkpoint**

Если `.git` есть:

```powershell
git add apps/api/WeatherForecast.cs apps/api/Controllers/WeatherForecastController.cs
git commit -m "chore: remove weather forecast template"
```

Если `.git` отсутствует, перечислить удаленные файлы в отчете.

## Task 4: Добавить health endpoint через TDD

**Files:**

- Create: `tests/LineCom.Api.Tests/System/HealthEndpointTests.cs`
- Create: `apps/api/Modules/System/Controllers/HealthController.cs`
- Modify: `apps/api/Program.cs`

- [ ] **Step 1: Написать failing integration test**

Create `tests/LineCom.Api.Tests/System/HealthEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LineCom.Api.Tests.System;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOkResponse()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/system/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("LineCom.Api", body.Service);
    }

    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 2: Запустить тест и убедиться, что он падает**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter GetHealth_ReturnsOkResponse
```

Expected: FAIL с 404 или ошибкой доступа к `Program`.

- [ ] **Step 3: Добавить health controller**

Create `apps/api/Modules/System/Controllers/HealthController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.System.Controllers;

[ApiController]
[Route("api/public/system")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse("ok", "LineCom.Api"));
    }
}

public sealed record HealthResponse(string Status, string Service);
```

- [ ] **Step 4: Обновить Program.cs**

Replace `apps/api/Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
```

- [ ] **Step 5: Запустить тест**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter GetHealth_ReturnsOkResponse
```

Expected: PASS.

- [ ] **Step 6: Запустить весь build**

Run:

```powershell
dotnet build LineCom.sln
```

Expected: PASS.

- [ ] **Step 7: Checkpoint**

Если `.git` есть:

```powershell
git add apps/api/Program.cs apps/api/Modules/System/Controllers/HealthController.cs tests/LineCom.Api.Tests/System/HealthEndpointTests.cs
git commit -m "feat: add public health endpoint"
```

Если `.git` отсутствует, перечислить созданные и измененные файлы в отчете.

## Task 5: Добавить database foundation без открытия реального соединения в тестах

**Files:**

- Create: `apps/api/Infrastructure/Database/IDbConnectionFactory.cs`
- Create: `apps/api/Infrastructure/Database/NpgsqlConnectionFactory.cs`
- Create: `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`
- Modify: `apps/api/Program.cs`
- Modify: `apps/api/appsettings.json`
- Modify: `apps/api/appsettings.Development.json`
- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/DatabaseRegistrationTests.cs`

- [ ] **Step 1: Написать failing tests для регистрации БД**

Create `tests/LineCom.Api.Tests/Infrastructure/Database/DatabaseRegistrationTests.cs`:

```csharp
using LineCom.Api.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class DatabaseRegistrationTests
{
    [Fact]
    public void AddDatabase_Throws_WhenConnectionStringMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDatabase(configuration));

        Assert.Equal("Connection string 'Default' is not configured.", exception.Message);
    }

    [Fact]
    public void AddDatabase_RegistersDataSourceAndConnectionFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddDatabase(configuration);

        using var provider = services.BuildServiceProvider();
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        var factory = provider.GetRequiredService<IDbConnectionFactory>();

        Assert.NotNull(dataSource);
        Assert.NotNull(factory);
    }
}
```

- [ ] **Step 2: Запустить тест и убедиться, что он падает**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter DatabaseRegistrationTests
```

Expected: FAIL, потому что `LineCom.Api.Infrastructure.Database` еще не существует.

- [ ] **Step 3: Добавить интерфейс connection factory**

Create `apps/api/Infrastructure/Database/IDbConnectionFactory.cs`:

```csharp
using Npgsql;

namespace LineCom.Api.Infrastructure.Database;

public interface IDbConnectionFactory
{
    ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Добавить Npgsql реализацию**

Create `apps/api/Infrastructure/Database/NpgsqlConnectionFactory.cs`:

```csharp
using Npgsql;

namespace LineCom.Api.Infrastructure.Database;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Добавить регистрацию сервисов БД**

Create `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`:

```csharp
using Npgsql;

namespace LineCom.Api.Infrastructure.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' is not configured.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }
}
```

- [ ] **Step 6: Подключить AddDatabase в Program.cs**

Modify `apps/api/Program.cs`:

```csharp
using LineCom.Api.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
```

- [ ] **Step 7: Добавить connection string в appsettings**

Modify `apps/api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Modify `apps/api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 8: Запустить тесты регистрации**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter DatabaseRegistrationTests
```

Expected: PASS.

- [ ] **Step 9: Запустить health test**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter GetHealth_ReturnsOkResponse
```

Expected: PASS. Тест не должен пытаться открыть PostgreSQL-соединение.

- [ ] **Step 10: Checkpoint**

Если `.git` есть:

```powershell
git add apps/api/Infrastructure/Database apps/api/Program.cs apps/api/appsettings.json apps/api/appsettings.Development.json tests/LineCom.Api.Tests/Infrastructure/Database
git commit -m "feat: add postgres connection foundation"
```

Если `.git` отсутствует, перечислить измененные файлы в отчете.

## Task 6: Добавить DbUp migration runner

**Files:**

- Create: `apps/dbmigrator/LineCom.DbMigrator.csproj`
- Create: `apps/dbmigrator/Program.cs`
- Create: `apps/dbmigrator/Migrations/001_extensions.sql`
- Modify: `LineCom.sln`

- [ ] **Step 1: Создать console project**

Run:

```powershell
dotnet new console -n LineCom.DbMigrator -o apps/dbmigrator
```

Expected: создан `apps/dbmigrator/LineCom.DbMigrator.csproj`.

- [ ] **Step 2: Добавить DbUp PostgreSQL package**

Run:

```powershell
dotnet add apps/dbmigrator/LineCom.DbMigrator.csproj package dbup-postgresql --version 7.0.1
```

Expected: `.csproj` содержит `PackageReference Include="dbup-postgresql" Version="7.0.1"`.

- [ ] **Step 3: Добавить migrator project в solution**

Run:

```powershell
dotnet sln LineCom.sln add apps/dbmigrator/LineCom.DbMigrator.csproj
```

Expected: `LineCom.sln` содержит `LineCom.DbMigrator`.

- [ ] **Step 4: Настроить embedded SQL migrations**

Replace `apps/dbmigrator/LineCom.DbMigrator.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="dbup-postgresql" Version="7.0.1" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Migrations/**/*.sql" />
  </ItemGroup>

</Project>
```

After replacing, run:

```powershell
dotnet restore apps/dbmigrator/LineCom.DbMigrator.csproj
```

Expected: restore succeeds and package version remains concrete in assets.

- [ ] **Step 5: Добавить первую миграцию**

Create `apps/dbmigrator/Migrations/001_extensions.sql`:

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
```

- [ ] **Step 6: Добавить Program.cs migrator**

Replace `apps/dbmigrator/Program.cs` with:

```csharp
using System.Reflection;
using DbUp;

var connectionString = GetConnectionString(args);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        Assembly.GetExecutingAssembly(),
        scriptName => scriptName.Contains(".Migrations.") && scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
    .JournalToPostgresqlTable("public", "schema_versions")
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

Console.WriteLine("Database migrations applied successfully.");
return 0;

static string GetConnectionString(string[] args)
{
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    {
        return args[0];
    }

    var fromEnvironment = Environment.GetEnvironmentVariable("LINECOM_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(fromEnvironment))
    {
        return fromEnvironment;
    }

    throw new InvalidOperationException("Connection string is required. Pass it as first argument or set LINECOM_CONNECTION_STRING.");
}
```

- [ ] **Step 7: Build migrator**

Run:

```powershell
dotnet build apps/dbmigrator/LineCom.DbMigrator.csproj
```

Expected: PASS.

- [ ] **Step 8: Проверить ошибку без connection string**

Run:

```powershell
dotnet run --project apps/dbmigrator/LineCom.DbMigrator.csproj
```

Expected: process exits with code `1` or unhandled InvalidOperationException message containing `Connection string is required`.

- [ ] **Step 9: Checkpoint**

Если `.git` есть:

```powershell
git add LineCom.sln apps/dbmigrator
git commit -m "feat: add dbup migration runner"
```

Если `.git` отсутствует, перечислить созданные файлы в отчете.

## Task 7: Добавить единый формат API ошибок

**Files:**

- Create: `apps/api/Shared/Errors/ApiErrorResponse.cs`
- Create: `apps/api/Shared/Errors/ApiException.cs`
- Create: `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`
- Modify: `apps/api/Program.cs`
- Create: `tests/LineCom.Api.Tests/Shared/Errors/ApiExceptionMiddlewareTests.cs`

- [ ] **Step 1: Написать failing middleware tests**

Create `tests/LineCom.Api.Tests/Shared/Errors/ApiExceptionMiddlewareTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LineCom.Api.Tests.Shared.Errors;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_MapsApiException_ToConfiguredStatusCode()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new ApiException("catalog.not_found", "Товар не найден.", StatusCodes.Status404NotFound),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(body);
        Assert.Equal("catalog.not_found", body.Code);
        Assert.Equal("Товар не найден.", body.Message);
    }

    [Fact]
    public async Task InvokeAsync_MapsUnhandledException_ToInternalError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Database password leaked in exception."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(body);
        Assert.Equal("internal_error", body.Code);
        Assert.Equal("Внутренняя ошибка сервера.", body.Message);
    }
}
```

- [ ] **Step 2: Запустить тест и убедиться, что он падает**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter ApiExceptionMiddlewareTests
```

Expected: FAIL, потому что `LineCom.Api.Shared.Errors` еще не существует.

- [ ] **Step 3: Добавить ApiErrorResponse**

Create `apps/api/Shared/Errors/ApiErrorResponse.cs`:

```csharp
namespace LineCom.Api.Shared.Errors;

public sealed record ApiErrorResponse(string Code, string Message);
```

- [ ] **Step 4: Добавить ApiException**

Create `apps/api/Shared/Errors/ApiException.cs`:

```csharp
namespace LineCom.Api.Shared.Errors;

public sealed class ApiException : Exception
{
    public ApiException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}
```

- [ ] **Step 5: Добавить middleware**

Create `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`:

```csharp
using System.Text.Json;

namespace LineCom.Api.Shared.Errors;

public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Code, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "Внутренняя ошибка сервера.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write API error response because the response has already started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse(code, message);
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions, context.RequestAborted);
    }
}
```

- [ ] **Step 6: Подключить middleware в Program.cs**

Modify `apps/api/Program.cs`:

```csharp
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Shared.Errors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
```

- [ ] **Step 7: Запустить middleware tests**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter ApiExceptionMiddlewareTests
```

Expected: PASS.

- [ ] **Step 8: Запустить весь test suite**

Run:

```powershell
dotnet test LineCom.sln
```

Expected: PASS.

- [ ] **Step 9: Checkpoint**

Если `.git` есть:

```powershell
git add apps/api/Shared/Errors apps/api/Program.cs tests/LineCom.Api.Tests/Shared/Errors
git commit -m "feat: add api error middleware"
```

Если `.git` отсутствует, перечислить созданные и измененные файлы в отчете.

## Task 8: Документировать запуск foundation

**Files:**

- Create: `vault/Человекочитаемое/Backend foundation.md`

- [ ] **Step 1: Создать документ**

Create `vault/Человекочитаемое/Backend foundation.md`:

```markdown
# Backend foundation

## Назначение

Backend foundation задает релизную основу API LineCom:

- ASP.NET Core Web API;
- модульная структура;
- Npgsql без Entity Framework;
- Dapper для SQL-маппинга;
- DbUp для SQL-миграций;
- единый формат ошибок;
- публичный health endpoint.

## Проверки

Запуск сборки:

```powershell
dotnet build LineCom.sln
```

Запуск тестов:

```powershell
dotnet test LineCom.sln
```

Запуск API локально:

```powershell
dotnet run --project apps/api/LineCom.Api.csproj
```

Health endpoint:

```text
GET /api/public/system/health
```

Ожидаемый ответ:

```json
{
  "status": "ok",
  "service": "LineCom.Api"
}
```

## Миграции

Миграции выполняются отдельным runner:

```powershell
dotnet run --project apps/dbmigrator/LineCom.DbMigrator.csproj -- "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom"
```

Можно передать connection string через переменную окружения:

```powershell
$env:LINECOM_CONNECTION_STRING="Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom"
dotnet run --project apps/dbmigrator/LineCom.DbMigrator.csproj
```

## Правила качества

- Entity Framework не используется.
- SQL-запросы должны быть параметризованы.
- Миграции пишутся SQL-скриптами.
- Технический долг не оставляется намеренно.
- Перед завершением задачи обязательно запускать build и tests.
```

- [ ] **Step 2: Проверить документ на запрещенные маркеры незавершенности**

Run:

```powershell
$pattern = ('TO' + 'DO') + '|' + ('TB' + 'D')
rg -n $pattern vault/Человекочитаемое/Backend foundation.md
```

Expected: no matches.

- [ ] **Step 3: Checkpoint**

Если `.git` есть:

```powershell
git add "vault/Человекочитаемое/Backend foundation.md"
git commit -m "docs: document backend foundation"
```

Если `.git` отсутствует, перечислить созданный документ в отчете.

## Task 9: Финальная проверка foundation

**Files:**

- Verify: `LineCom.sln`
- Verify: `apps/api`
- Verify: `apps/dbmigrator`
- Verify: `tests/LineCom.Api.Tests`
- Verify: `vault/Человекочитаемое/Backend foundation.md`

- [ ] **Step 1: Проверить отсутствие шаблонного WeatherForecast**

Run:

```powershell
rg -n "WeatherForecast" apps tests
```

Expected: no matches.

- [ ] **Step 2: Проверить отсутствие Entity Framework**

Run:

```powershell
rg -n "EntityFramework|DbContext|UseNpgsql" apps tests
```

Expected: no matches.

- [ ] **Step 3: Проверить отсутствие маркеров незавершенности**

Run:

```powershell
$pattern = ('TO' + 'DO') + '|' + ('TB' + 'D')
rg -n $pattern apps tests vault/Человекочитаемое
```

Expected: no matches except intentional project rule text that names these markers as forbidden.

- [ ] **Step 4: Restore**

Run:

```powershell
dotnet restore LineCom.sln
```

Expected: PASS.

- [ ] **Step 5: Build**

Run:

```powershell
dotnet build LineCom.sln
```

Expected: PASS.

- [ ] **Step 6: Tests**

Run:

```powershell
dotnet test LineCom.sln
```

Expected: PASS.

- [ ] **Step 7: Проверка на технический долг**

Проверить вручную и записать в отчет исполнителя:

```text
Проверка техдолга:
- временных архитектурных решений нет;
- Entity Framework не добавлен;
- миграции вынесены в DbUp runner;
- connection string не захардкожен в коде;
- WeatherForecast удален;
- API errors не раскрывают внутренние exception messages;
- build и tests проходят.
```

- [ ] **Step 8: Финальный checkpoint**

Если `.git` есть:

```powershell
git status --short
```

Expected: no uncommitted changes.

Если `.git` отсутствует, финальный отчет должен перечислить все созданные, измененные и удаленные файлы.

## Self-Review

Spec coverage:

- модульный backend foundation покрыт задачами 3-7;
- Npgsql/Dapper покрыты задачами 2 и 5;
- DbUp SQL migrations покрыты задачей 6;
- health endpoint покрыт задачей 4;
- единый формат ошибок покрыт задачей 7;
- документация запуска покрыта задачей 8;
- проверка отсутствия техдолга покрыта задачей 9.

Placeholder scan:

- План не содержит явных маркеров незавершенности;
- Все создаваемые файлы имеют конкретное содержимое;
- Все команды имеют ожидаемый результат.

Type consistency:

- `IDbConnectionFactory` используется в тестах и реализации с одинаковым namespace `LineCom.Api.Infrastructure.Database`;
- `ApiErrorResponse` используется в middleware tests и middleware с одинаковым namespace `LineCom.Api.Shared.Errors`;
- health response в API и тесте согласован по JSON-полям `status` и `service`;
- DbUp runner использует `LINECOM_CONNECTION_STRING` и аргумент командной строки как единственные источники connection string.
