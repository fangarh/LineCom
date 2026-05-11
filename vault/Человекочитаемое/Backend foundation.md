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

Запуск restore:

```powershell
dotnet restore LineCom.sln
```

Сборка API и тестов:

```powershell
dotnet build LineCom.sln
```

Сборка migration runner:

```powershell
dotnet build apps/dbmigrator/LineCom.DbMigrator.csproj
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

## Локальные строки подключения

Реальная строка подключения к песочной или локальной PostgreSQL БД не хранится в git.

Для запуска API используется стандартная переменная конфигурации ASP.NET Core:

```powershell
$env:ConnectionStrings__Default="<connection string>"
dotnet run --project apps/api/LineCom.Api.csproj
```

Для запуска мигратора используется:

```powershell
$env:LINECOM_CONNECTION_STRING="<connection string>"
dotnet run --project apps/dbmigrator/LineCom.DbMigrator.csproj
```

Для PostgreSQL-интеграционных тестов миграций и Dapper-запросов используется:

```powershell
$env:LINECOM_TEST_CONNECTION_STRING="<disposable test database connection string>"
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj
```

Тестовая БД считается одноразовой: интеграционные тесты могут удалять и пересоздавать схему `public`.

## Правила качества

- Entity Framework не используется.
- SQL-запросы должны быть параметризованы.
- Миграции пишутся SQL-скриптами.
- Migration runner собирается и проверяется отдельной командой.
- Технический долг не оставляется намеренно.
- Перед завершением задачи обязательно запускать restore, build и tests.
