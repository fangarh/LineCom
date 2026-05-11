# Admin Request Processing iterations

## Статус

Документационная часть Task 9 закрывает контракт реализованного среза `Admin Request Processing`.

Реализация к моменту обновления документации включает:

- backend endpoints `/api/admin/requests`, `/api/admin/requests/{number}`, `PATCH /api/admin/requests/{number}/status`, `PUT /api/admin/requests/{number}/internal-comment`;
- отдельные admin DTO для списка, карточки, смены статуса и внутреннего комментария;
- доступ только для активных аутентифицированных пользователей с ролями `seller` и `admin`;
- CSRF-защиту изменяющих endpoints;
- четыре релизных статуса заявки: `new`, `in_progress`, `completed`, `cancelled`;
- миграцию старого `quoted` в `in_progress` и исключение `quoted` из релизного справочника;
- отсутствие `internalComment` и staff-only истории комментариев в клиентских endpoints.

## Границы среза

Срез не включает цены, оплату, счета, отгрузку, файлы коммерческих предложений, фиксацию заказа, редактирование состава заявки, назначение менеджера, уведомления, интеграцию с 1С и админский каталог.

## Документационные изменения

- Создан `Admin Request Processing API.md`.
- Обновлена продуктовая модель: админские возможности в этом срезе ограничены обработкой заявок.
- Обновлен `Auth Request Core API.md`: статус отмены приведен к `cancelled`, admin processing вынесен в отдельный документ.

## Проверки

Перед коммитом требуется выполнить:

```powershell
rg -n "quoted|canceled|cancelled|Admin Request Processing" vault/Человекочитаемое
```

Ожидаемый результат для документов этого среза:

- `cancelled` присутствует как единственное машинное имя статуса отмены;
- `canceled` не описывает релизный статус заявки в отредактированных документах;
- `quoted` встречается только как явно удаленный и мигрированный в `in_progress` статус в документации admin processing.

Фактическая финальная проверка среза:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1 --no-build
npm.cmd run lint
npm.cmd test
npm.cmd run build
npm.cmd test -- src/components/account/request-list.test.tsx
rg -n "canceled|cancelled" apps/front/src apps/api tests/LineCom.Api.Tests apps/dbmigrator/Migrations
```

Результат:

- backend build: PASS, 0 warnings, 0 errors;
- backend tests: PASS, 392/392;
- frontend lint: PASS;
- frontend tests: PASS, 78/78; Vitest печатает jsdom-сообщение `Not implemented: navigation to another Document`, процесс завершается с кодом 0;
- frontend production build: PASS, маршруты `/admin/requests` и `/admin/requests/[number]` собираются;
- targeted account request list test: PASS, 4/4;
- scope search: в реализации используется `cancelled`; `canceled` остался только в отрицательной frontend-проверке, что старый код статуса не показывается.

Browser QA:

- API запущен локально на `http://127.0.0.1:8080`, frontend production server на `http://127.0.0.1:4300`;
- неавторизованная сессия на `/admin/requests` и `/admin/requests/REQ-2026-0001` перенаправляется на `/auth/login` с корректным `returnTo`;
- staff-сценарий проверен через Playwright network mocking без записи в dev database: список заявок и карточка заявки рендерятся, статусные опции равны `new`, `in_progress`, `completed`, `cancelled`;
- смена статуса отправляет `PATCH /api/admin/requests/{number}/status` с body `{"status":"in_progress"}` и `X-CSRF-Token`;
- сохранение внутреннего комментария отправляет `PUT /api/admin/requests/{number}/internal-comment` с body `{"internalComment":"Новый внутренний комментарий"}` и `X-CSRF-Token`;
- в чистой browser-smoke вкладке после mocked staff flow нет console errors/warnings.
