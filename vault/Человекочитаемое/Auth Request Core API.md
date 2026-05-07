# Auth + Request Core API

## Назначение

Этот документ фиксирует реализованный контракт этапа `Auth + Request Core`.

API предназначен для регистрации клиента, входа, чтения текущего пользователя, ведения профиля и необязательной организации, создания коммерческой заявки авторизованным пользователем и чтения своих заявок. Контракт строится поверх PostgreSQL, Npgsql и Dapper. Entity Framework не используется.

Публичные цены, онлайн-оплата, счета, отгрузки, интеграция с 1С, восстановление пароля, одноразовые коды входа, уведомления и админская смена статусов не входят в этот этап.

## Статус реализации

Контракт зафиксирован 2026-05-07 в рамках итерации 1 плана `Auth Request Core iterations.md`.
Backend-реализация закрыта 2026-05-07 в рамках итерации 9.

Реализованные endpoints:

- `POST /api/auth/register`;
- `POST /api/auth/login`;
- `GET /api/auth/me`;
- `GET /api/account/profile`;
- `PUT /api/account/profile`;
- `PUT /api/account/organization`;
- `POST /api/account/requests`;
- `GET /api/account/requests`;
- `GET /api/account/requests/{number}`.

Финальная сверка подтвердила, что реализация использует PostgreSQL через Npgsql и Dapper, SQL-миграции DbUp, HTTP-only cookie auth, CSRF-header для изменяющих защищенных endpoints, транзакционное создание заявки с backend-номером и не возвращает публичные цены.

## Общие правила

- Ответы возвращаются в JSON с `camelCase` именами полей.
- Все контролируемые ошибки возвращаются через `ApiErrorResponse`.
- Внутренние exception messages не попадают в публичный ответ.
- SQL-запросы реализации должны быть параметризованы.
- Контроллеры не содержат SQL и предметную логику.
- Auth строится на серверной HTTP-only cookie.
- Клиентские endpoints заявок требуют авторизованного и активного пользователя.
- `userId` для защищенных endpoints берется только из auth-контекста, а не из тела запроса или URL.
- Пароль не возвращается в API и не хранится в открытом виде.
- Заявка создается только backend в одной транзакции с номером, позициями, снимками и историей.
- Номер заявки генерируется только backend в формате `ЗКYY-0001`.
- Количество позиции заявки означает количество единиц продажи товара, а не метраж.
- Публичные цены не добавляются в DTO и не возвращаются в ответах.

## Cookie-auth и CSRF

Успешная регистрация и вход устанавливают auth cookie.

Правила auth cookie:

- имя cookie: `linecom_auth`;
- cookie содержит только серверное auth-состояние, клиент не читает ее значение;
- `HttpOnly = true`;
- `Secure = true` в production;
- `SameSite = Lax`;
- `Path = /`;
- срок жизни задается серверной auth-конфигурацией и не является частью публичного DTO.

CSRF-совместимость:

- изменяющие endpoints с cookie-auth требуют CSRF-токен в заголовке `X-CSRF-Token`;
- auth-ответы возвращают `csrfToken`, который frontend использует для следующих изменяющих запросов;
- CSRF-токен не является auth-токеном и не заменяет cookie;
- отсутствие или несовпадение CSRF-токена возвращает `403 auth.forbidden`;
- `GET` endpoints не изменяют состояние сервера.

## Единый формат ошибок

Формат контролируемой ошибки:

```json
{
  "code": "auth.unauthorized",
  "message": "Требуется вход в аккаунт."
}
```

Коды ошибок этапа:

| HTTP | Code | Message |
| --- | --- | --- |
| `400` | `validation.invalid_request` | `Некорректные данные запроса.` |
| `400` | `auth.invalid_contact` | `Укажите email или телефон.` |
| `400` | `auth.invalid_password` | `Некорректный пароль.` |
| `401` | `auth.invalid_credentials` | `Неверный логин или пароль.` |
| `401` | `auth.unauthorized` | `Требуется вход в аккаунт.` |
| `403` | `auth.forbidden` | `Недостаточно прав.` |
| `403` | `auth.user_inactive` | `Аккаунт отключен.` |
| `409` | `auth.user_already_exists` | `Пользователь с таким email или телефоном уже существует.` |
| `400` | `request.invalid_items` | `Некорректный состав заявки.` |
| `400` | `request.product_not_available` | `Товар недоступен для заявки.` |
| `404` | `request.not_found` | `Заявка не найдена.` |
| `400` | `request.invalid_status` | `Некорректный статус заявки.` |
| `500` | `internal_error` | `Внутренняя ошибка сервера.` |

`auth.invalid_credentials` не раскрывает, существует ли email или телефон. `request.not_found` используется и для отсутствующей заявки, и для чужой заявки текущего пользователя.

## Справочные значения

`role`:

- `customer` - клиент;
- `seller` - продавец;
- `admin` - администратор.

`request.source` для клиентских endpoints:

- `cart` - заявка из корзины;
- `quick_order` - быстрый заказ авторизованного пользователя.

`admin_created` резервируется для будущего админского контура и не принимается клиентским endpoint создания заявки.

`request.status`:

- `new` - новая;
- `in_progress` - в работе;
- `completed` - завершено;
- `canceled` - отменено.

На этом этапе клиент создает только заявку со статусом `new`. Смена статуса сотрудником входит в следующий этап.

## Auth API

Базовый префикс маршрутов: `/api/auth`.

### POST `/api/auth/register`

Регистрирует клиента и устанавливает auth cookie.

Тело запроса:

```json
{
  "name": "Иван Петров",
  "email": "ivan@example.com",
  "phone": "+7 900 000-00-00",
  "password": "secure-password"
}
```

Правила:

- `name` обязателен, после обрезки пробелов не пустой;
- нужен хотя бы один контакт: `email` или `phone`;
- `email`, если указан, должен быть корректным email;
- `phone`, если указан, должен быть пригоден для нормализации и содержать от 4 до 32 символов;
- `password` обязателен, длина от 8 до 128 символов;
- роль нового пользователя всегда `customer`;
- дубль непустого `email` или `phone` запрещен;
- организация при регистрации не создается.

Ответ `201 Created`:

```json
{
  "user": {
    "id": "1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6",
    "name": "Иван Петров",
    "email": "ivan@example.com",
    "phone": "+79000000000",
    "role": "customer"
  },
  "csrfToken": "csrf-token"
}
```

DTO:

```text
RegisterRequest
- name: string
- email: string | null
- phone: string | null
- password: string

AuthSessionDto
- user: CurrentUserDto
- csrfToken: string

CurrentUserDto
- id: uuid
- name: string
- email: string | null
- phone: string | null
- role: string
```

Ошибки:

- `400 validation.invalid_request`;
- `400 auth.invalid_contact`;
- `400 auth.invalid_password`;
- `409 auth.user_already_exists`.

### POST `/api/auth/login`

Выполняет вход по email или телефону и паролю, затем устанавливает auth cookie.

Тело запроса:

```json
{
  "login": "ivan@example.com",
  "password": "secure-password"
}
```

Правила:

- `login` принимает email или телефон;
- поиск по email и телефону не должен раскрывать существование контакта;
- неактивный пользователь не получает auth cookie;
- успешный вход возвращает текущего пользователя и CSRF-токен.

Ответ `200 OK`:

```json
{
  "user": {
    "id": "1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6",
    "name": "Иван Петров",
    "email": "ivan@example.com",
    "phone": "+79000000000",
    "role": "customer"
  },
  "csrfToken": "csrf-token"
}
```

DTO:

```text
LoginRequest
- login: string
- password: string

AuthSessionDto
- user: CurrentUserDto
- csrfToken: string
```

Ошибки:

- `400 validation.invalid_request`;
- `401 auth.invalid_credentials`;
- `403 auth.user_inactive`.

### GET `/api/auth/me`

Возвращает текущего пользователя.

Ответ `200 OK`:

```json
{
  "user": {
    "id": "1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6",
    "name": "Иван Петров",
    "email": "ivan@example.com",
    "phone": "+79000000000",
    "role": "customer"
  },
  "csrfToken": "csrf-token"
}
```

Ошибки:

- `401 auth.unauthorized`;
- `403 auth.user_inactive`.

## Account Profile API

Базовый префикс маршрутов: `/api/account`.

Все endpoints профиля требуют авторизованного и активного пользователя.

### GET `/api/account/profile`

Возвращает профиль текущего пользователя и его организацию, если она заполнена.

Ответ `200 OK`:

```json
{
  "user": {
    "id": "1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6",
    "name": "Иван Петров",
    "email": "ivan@example.com",
    "phone": "+79000000000",
    "role": "customer"
  },
  "organization": {
    "name": "ООО Сеть",
    "inn": "7700000000",
    "contactPerson": "Иван Петров",
    "phone": "+79000000000",
    "email": "sales@example.com",
    "comment": "Основная организация"
  }
}
```

DTO:

```text
AccountProfileDto
- user: CurrentUserDto
- organization: AccountOrganizationDto | null

AccountOrganizationDto
- name: string
- inn: string | null
- contactPerson: string | null
- phone: string | null
- email: string | null
- comment: string | null
```

Ошибки:

- `401 auth.unauthorized`;
- `403 auth.user_inactive`.

### PUT `/api/account/profile`

Обновляет базовый профиль текущего пользователя.

Тело запроса:

```json
{
  "name": "Иван Петров",
  "email": "ivan@example.com",
  "phone": "+7 900 000-00-00"
}
```

Правила:

- `name` обязателен;
- нужен хотя бы один контакт: `email` или `phone`;
- дубль непустого `email` или `phone` у другого пользователя запрещен;
- обновление профиля не меняет снимки в уже созданных заявках.

Ответ `200 OK` возвращает `CurrentUserDto`.

Ошибки:

- `400 validation.invalid_request`;
- `400 auth.invalid_contact`;
- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`;
- `409 auth.user_already_exists`.

### PUT `/api/account/organization`

Создает или обновляет необязательную организацию текущего пользователя.

Тело запроса:

```json
{
  "name": "ООО Сеть",
  "inn": "7700000000",
  "contactPerson": "Иван Петров",
  "phone": "+7 900 000-00-00",
  "email": "sales@example.com",
  "comment": "Основная организация"
}
```

Правила:

- у пользователя может быть одна организация;
- `name` обязателен для создания организации;
- `inn`, если указан, хранится как строка;
- `email`, если указан, должен быть корректным email;
- обновление организации не меняет снимки в уже созданных заявках.

Ответ `200 OK` возвращает `AccountOrganizationDto`.

Ошибки:

- `400 validation.invalid_request`;
- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`.

## Customer Requests API

Базовый префикс маршрутов: `/api/account/requests`.

Все endpoints заявок требуют авторизованного и активного пользователя.

### POST `/api/account/requests`

Создает заявку текущего пользователя.

Тело запроса:

```json
{
  "source": "cart",
  "customerComment": "Нужна консультация по срокам поставки.",
  "items": [
    {
      "productId": "e9c9e401-2f72-49a6-95bd-4e649cedeb3a",
      "quantity": 2,
      "customerComment": "Подойдет аналог, если быстрее."
    }
  ]
}
```

Правила:

- `source` принимает только `cart` или `quick_order`;
- в заявке должна быть минимум одна позиция;
- `productId` обязателен для каждой позиции;
- `quantity` - целое число единиц продажи, минимум `1`;
- товар должен быть опубликован и находиться в активной категории;
- backend сохраняет снимки товара, контактов пользователя и организации текущего пользователя;
- если организация не заполнена, организационный снимок сохраняется пустым;
- создается история с событием `created`;
- статус новой заявки всегда `new`;
- номер заявки генерируется backend внутри транзакции.

Ответ `201 Created`:

```json
{
  "number": "ЗК26-0001",
  "status": {
    "code": "new",
    "label": "Новая"
  },
  "source": "cart",
  "customerComment": "Нужна консультация по срокам поставки.",
  "createdAt": "2026-05-07T10:15:30Z",
  "items": [
    {
      "productId": "e9c9e401-2f72-49a6-95bd-4e649cedeb3a",
      "productName": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      "productSku": "LC-UTP5E-CU-305",
      "saleUnit": {
        "code": "coil",
        "label": "бухта"
      },
      "unitQuantity": "305 м",
      "quantity": 2,
      "customerComment": "Подойдет аналог, если быстрее."
    }
  ]
}
```

DTO:

```text
CreateRequestCommand
- source: string
- customerComment: string | null
- items: CreateRequestItemCommand[]

CreateRequestItemCommand
- productId: uuid
- quantity: integer
- customerComment: string | null

CustomerRequestDetailDto
- number: string
- status: RequestStatusDto
- source: string
- customerComment: string | null
- createdAt: datetime
- items: CustomerRequestItemDto[]

CustomerRequestItemDto
- productId: uuid
- productName: string
- productSku: string | null
- saleUnit: PublicCodeLabelDto
- unitQuantity: string
- quantity: integer
- customerComment: string | null

RequestStatusDto
- code: string
- label: string

PublicCodeLabelDto
- code: string
- label: string
```

Ошибки:

- `400 validation.invalid_request`;
- `400 request.invalid_items`;
- `400 request.product_not_available`;
- `401 auth.unauthorized`;
- `403 auth.forbidden`;
- `403 auth.user_inactive`.

### GET `/api/account/requests`

Возвращает список заявок текущего пользователя.

Параметры:

| Query | Тип | Обязателен | Правило |
| --- | --- | --- | --- |
| `page` | integer | нет | Минимум `1`, значение по умолчанию `1`. |
| `pageSize` | integer | нет | От `1` до `60`, значение по умолчанию `20`. |
| `status` | string | нет | Один из кодов статусов заявки. |

Сортировка: `createdAt desc`, затем `number desc`.

Ответ `200 OK`:

```json
{
  "items": [
    {
      "number": "ЗК26-0001",
      "status": {
        "code": "new",
        "label": "Новая"
      },
      "source": "cart",
      "itemsCount": 1,
      "customerComment": "Нужна консультация по срокам поставки.",
      "createdAt": "2026-05-07T10:15:30Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

DTO:

```text
CustomerRequestListResponse
- items: CustomerRequestListItemDto[]
- page: number
- pageSize: number
- totalItems: number
- totalPages: number

CustomerRequestListItemDto
- number: string
- status: RequestStatusDto
- source: string
- itemsCount: number
- customerComment: string | null
- createdAt: datetime
```

Ошибки:

- `400 validation.invalid_request`;
- `400 request.invalid_status`;
- `401 auth.unauthorized`;
- `403 auth.user_inactive`.

### GET `/api/account/requests/{number}`

Возвращает карточку заявки текущего пользователя по публичному номеру.

Правила:

- URL использует `number`, а не внутренний `id`;
- пользователь видит только свои заявки;
- чужая заявка возвращается как `request.not_found`;
- ответ содержит снимки, которые были сохранены при создании заявки;
- ответ не содержит публичных цен.

Ответ `200 OK`:

```json
{
  "number": "ЗК26-0001",
  "status": {
    "code": "new",
    "label": "Новая"
  },
  "source": "cart",
  "customer": {
    "name": "Иван Петров",
    "email": "ivan@example.com",
    "phone": "+79000000000"
  },
  "organization": {
    "name": "ООО Сеть",
    "inn": "7700000000",
    "contactPerson": "Иван Петров"
  },
  "customerComment": "Нужна консультация по срокам поставки.",
  "createdAt": "2026-05-07T10:15:30Z",
  "items": [
    {
      "productId": "e9c9e401-2f72-49a6-95bd-4e649cedeb3a",
      "productName": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      "productSku": "LC-UTP5E-CU-305",
      "saleUnit": {
        "code": "coil",
        "label": "бухта"
      },
      "unitQuantity": "305 м",
      "quantity": 2,
      "customerComment": "Подойдет аналог, если быстрее."
    }
  ],
  "history": [
    {
      "event": "created",
      "message": "Заявка создана.",
      "createdAt": "2026-05-07T10:15:30Z"
    }
  ]
}
```

DTO:

```text
CustomerRequestDetailDto
- number: string
- status: RequestStatusDto
- source: string
- customer: RequestCustomerSnapshotDto
- organization: RequestOrganizationSnapshotDto | null
- customerComment: string | null
- createdAt: datetime
- items: CustomerRequestItemDto[]
- history: CustomerRequestHistoryDto[]

RequestCustomerSnapshotDto
- name: string
- email: string | null
- phone: string | null

RequestOrganizationSnapshotDto
- name: string
- inn: string | null
- contactPerson: string | null

CustomerRequestHistoryDto
- event: string
- message: string
- createdAt: datetime
```

Ошибки:

- `401 auth.unauthorized`;
- `403 auth.user_inactive`;
- `404 request.not_found`.

## Границы этапа

В этот контракт входят:

- регистрация клиента по email или телефону и паролю;
- вход по email или телефону и паролю;
- текущий пользователь;
- профиль клиента;
- необязательная организация клиента;
- создание заявки авторизованным клиентом;
- список заявок текущего клиента;
- карточка заявки текущего клиента;
- снимки контактных, организационных и товарных данных на момент создания заявки;
- история создания заявки.

В этот контракт не входят:

- анонимные заявки;
- одноразовые коды входа;
- восстановление пароля;
- email/SMS-уведомления;
- публичные цены;
- онлайн-оплата;
- счета;
- отгрузки;
- интеграция с 1С;
- смена статусов заявки сотрудником;
- редактирование состава заявки продавцом;
- админские endpoints.

Эти контуры проектируются отдельными этапами без изменения продуктовых границ `Auth + Request Core`.

## Проверки для реализации

Минимальная реализация должна подтвердить контракт тестами:

- регистрация требует email или телефон;
- регистрация не допускает дубль email или телефона;
- пароль не возвращается и не хранится открытым текстом;
- вход отклоняет неверный пароль без раскрытия существования контакта;
- успешная регистрация и успешный вход устанавливают auth cookie;
- изменяющие защищенные endpoints требуют `X-CSRF-Token`;
- `GET /api/auth/me` требует auth;
- профиль и организация ограничены текущим пользователем;
- обновление профиля и организации не меняет исторические заявки;
- создание заявки требует auth;
- создание заявки требует минимум одну позицию;
- создание заявки отклоняет недоступный товар;
- создание заявки генерирует номер `ЗКYY-0001`;
- создание заявки сохраняет снимки товара, контактов и организации;
- чужая заявка возвращается как `request.not_found`;
- список заявок ограничен текущим пользователем;
- ответы заявок не содержат публичных цен.

## SEO/GEO влияние

Auth и account endpoints сами не создают индексируемые публичные страницы. Они не должны ухудшать SEO/GEO публичного каталога:

- публичные URL категорий и товаров остаются построенными на `slug`;
- заявки используют внутренний аккаунтный маршрут и публичный номер заявки, а не URL каталога;
- ответы заявок могут содержать товарные снимки, но не заменяют публичные карточки товаров;
- публичные цены не появляются в API заявок и не влияют на индексируемые страницы каталога.
