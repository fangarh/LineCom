# Admin Request Processing API

## Назначение

Документ фиксирует реализованный контракт этапа `Admin Request Processing`.

Срез отвечает только за обработку уже созданных заявок сотрудниками: просмотр общего списка, просмотр карточки заявки, смену статуса и ведение внутреннего комментария. В этот срез не входят публичные цены, платежи, счета, отгрузки, файлы коммерческих предложений, юридическая фиксация заказа, редактирование состава заявки, назначение менеджера, уведомления, интеграция с 1С и админский каталог.

Клиентские endpoints заявок документируются в `Auth Request Core API.md` и не возвращают `internalComment` или историю комментариев, предназначенную только для сотрудников.

## Доступ и безопасность

- Все endpoints имеют базовый префикс `/api/admin/requests`.
- Доступ разрешен только активному аутентифицированному пользователю с ролью `seller` или `admin`.
- Неаутентифицированный пользователь получает `401 auth.unauthorized`.
- Неактивный пользователь получает `403 auth.user_inactive`.
- Пользователь с ролью `customer` получает `403 auth.forbidden`.
- Изменяющие endpoints требуют CSRF-токен в заголовке `X-CSRF-Token`.
- `GET` endpoints не требуют CSRF-токен и не меняют состояние.
- Все ответы возвращаются в JSON с `camelCase` именами полей.

## Статусы заявки

Релизный набор статусов состоит только из четырех значений:

- `new` - новая;
- `in_progress` - в работе;
- `completed` - завершена;
- `cancelled` - отменена.

Статус `quoted` удален из релизной модели и мигрирован в `in_progress`. Значение `canceled` не используется.

## Endpoints

### GET `/api/admin/requests`

Возвращает общий список заявок для сотрудников.

Query-параметры:

- `page: int | null` - номер страницы, по умолчанию `1`;
- `pageSize: int | null` - размер страницы, по умолчанию `20`, максимум `60`;
- `status: string | null` - один из релизных статусов;
- `number: string | null` - поиск по номеру заявки;
- `contact: string | null` - поиск по контактным данным клиента;
- `organization: string | null` - поиск по организации.

Ответ `200 OK`: `AdminRequestListResponse`.

Ошибки:

- `400 request.invalid_status`;
- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`.

### GET `/api/admin/requests/{number}`

Возвращает карточку заявки по номеру.

Ответ `200 OK`: `AdminRequestDetailDto`.

Ошибки:

- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`;
- `404 request.not_found`.

### PATCH `/api/admin/requests/{number}/status`

Меняет статус заявки и возвращает обновленную карточку.

Тело запроса:

```json
{
  "status": "in_progress"
}
```

Правила:

- `status` обязателен;
- значение должно быть одним из `new`, `in_progress`, `completed`, `cancelled`;
- изменение фиксируется в истории заявки;
- endpoint требует CSRF-токен.

Ответ `200 OK`: `AdminRequestDetailDto`.

Ошибки:

- `400 validation.invalid_request`;
- `400 request.invalid_status`;
- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`;
- `404 request.not_found`.

### PUT `/api/admin/requests/{number}/internal-comment`

Обновляет внутренний комментарий сотрудников и возвращает обновленную карточку.

Тело запроса:

```json
{
  "internalComment": "Позвонить клиенту после 15:00."
}
```

Правила:

- пустая или состоящая только из пробелов строка сохраняется как `null`;
- комментарий доступен только в admin DTO;
- изменение фиксируется в истории заявки для сотрудников;
- endpoint требует CSRF-токен.

Ответ `200 OK`: `AdminRequestDetailDto`.

Ошибки:

- `400 validation.invalid_request`;
- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`;
- `404 request.not_found`.

## DTO

```text
AdminRequestListQuery
- page: int | null
- pageSize: int | null
- status: string | null
- number: string | null
- contact: string | null
- organization: string | null

AdminRequestListResponse
- items: AdminRequestListItemDto[]
- page: int
- pageSize: int
- totalItems: int
- totalPages: int

AdminRequestListItemDto
- number: string
- status: RequestStatusDto
- source: string
- itemsCount: int
- customer: RequestCustomerSnapshotDto
- organization: RequestOrganizationSnapshotDto | null
- customerComment: string | null
- internalComment: string | null
- createdAt: datetime
- updatedAt: datetime

AdminRequestDetailDto
- number: string
- status: RequestStatusDto
- source: string
- customer: RequestCustomerSnapshotDto
- organization: RequestOrganizationSnapshotDto | null
- customerComment: string | null
- internalComment: string | null
- createdAt: datetime
- updatedAt: datetime
- items: CustomerRequestItemDto[]
- history: CustomerRequestHistoryDto[]

UpdateAdminRequestStatusCommand
- status: string | null

UpdateAdminRequestInternalCommentCommand
- internalComment: string | null
```

Общие DTO `RequestStatusDto`, `RequestCustomerSnapshotDto`, `RequestOrganizationSnapshotDto`, `CustomerRequestItemDto` и `CustomerRequestHistoryDto` совпадают с request core-моделью. В admin-ответах история предназначена для сотрудников; клиентские endpoints не раскрывают `internalComment` и staff-only историю комментариев.
