# Public Catalog API

## Назначение

Этот документ фиксирует реализованный контракт публичного read-only API каталога LineCom.

API предназначен для публичных страниц категорий, карточек товаров, листинга, фильтров, SEO/GEO-страниц и будущего сравнения товаров. Контракт строится поверх PostgreSQL, Npgsql и Dapper. Публичные цены не возвращаются.

## Статус реализации

Этап `Public Catalog read API` реализован и проверен 2026-05-06.

Реализованные endpoints:

- `GET /api/public/catalog/categories`;
- `GET /api/public/catalog/categories/{slug}`;
- `GET /api/public/catalog/categories/{slug}/filters`;
- `GET /api/public/catalog/products`;
- `GET /api/public/catalog/products/{slug}`.

Проверка этапа:

- `dotnet restore LineCom.sln`;
- `dotnet build LineCom.sln --no-restore`;
- `dotnet test LineCom.sln --no-build --no-restore`: 177 тестов, 0 ошибок.
- opt-in PostgreSQL-интеграционные тесты Dapper-запросов публичного каталога запускаются при наличии `LINECOM_TEST_CONNECTION_STRING`.

## Общие правила

- Базовый префикс маршрутов: `/api/public/catalog`.
- Все endpoints каталога доступны без авторизации.
- Все endpoints публичного каталога используют только метод `GET`.
- `GET` не изменяет состояние сервера.
- SQL-запросы реализации должны быть параметризованы.
- Контроллеры не содержат SQL и предметную логику.
- Ответы возвращаются в JSON с `camelCase` именами полей.
- Публичная цена не добавляется в DTO и не возвращается в ответах.
- Внутренние идентификаторы могут возвращаться только когда они нужны frontend как стабильные технические ключи; публичная навигация строится по `slug`.
- Внутренние exception messages не попадают в публичный ответ.

## Правила видимости

Публичный каталог показывает только данные, которые пригодны для публикации:

- категории: только `is_active = true`;
- категории в меню: только `is_active = true` и `is_visible_in_menu = true`, если endpoint используется для меню;
- товары: только `publish_status = 'published'`;
- бренды: только `is_active = true`;
- характеристики: только `is_active = true`;
- характеристики в карточке товара: только `is_visible_in_product = true`;
- фильтры: только `is_filterable = true`;
- опции `select`-характеристик: только `is_active = true`;
- изображения товара: только связанные с активным `stored_files.status = 'active'` файлы назначения `product_image`;
- логотип бренда: только связанный с активным `stored_files.status = 'active'` файл назначения `brand_logo`.

Если опубликованный товар связан с неактивной категорией, товар не попадает в публичные списки и карточку. Если опубликованный товар связан с неактивным брендом, товар может отображаться, но поле `brand` возвращается как `null`, чтобы не публиковать неактивный бренд.

## Справочные значения

`availabilityStatus` возвращается как код и человекочитаемое название:

- `in_stock` - `В наличии`;
- `on_order` - `Под заказ`;
- `check_availability` - `Уточнить`.

`saleUnit` возвращается как код и человекочитаемое название:

- `coil` - `бухта`;
- `box` - `коробка`;
- `piece` - `штука`;
- `pack` - `упаковка`.

## Единый формат ошибок

Все контролируемые ошибки возвращаются в формате `ApiErrorResponse`:

```json
{
  "code": "catalog.category_not_found",
  "message": "Категория не найдена."
}
```

Коды ошибок публичного каталога:

| HTTP | Code | Message |
| --- | --- | --- |
| `400` | `catalog.invalid_pagination` | `Некорректные параметры пагинации.` |
| `400` | `catalog.invalid_sort` | `Некорректный параметр сортировки.` |
| `400` | `catalog.invalid_filter` | `Некорректный параметр фильтра.` |
| `404` | `catalog.category_not_found` | `Категория не найдена.` |
| `404` | `catalog.product_not_found` | `Товар не найден.` |
| `500` | `internal_error` | `Внутренняя ошибка сервера.` |

`404` используется и для физического отсутствия записи, и для записи, которая не проходит правила публичной видимости.

## Маршруты

### GET `/api/public/catalog/categories`

Возвращает дерево активных категорий для публичного каталога.

Сортировка на каждом уровне:

1. `sortOrder`;
2. `name`;
3. `slug`.

Ответ:

```json
{
  "items": [
    {
      "id": "6f830f45-0502-4cbf-8cda-f0ac8c74e7f1",
      "parentId": null,
      "name": "Витая пара",
      "slug": "vitaya-para",
      "h1": "Витая пара",
      "description": "Краткое описание категории.",
      "sortOrder": 10,
      "isVisibleInMenu": true,
      "children": [
        {
          "id": "dcd4f577-6076-4283-b256-30ea0822a3b2",
          "parentId": "6f830f45-0502-4cbf-8cda-f0ac8c74e7f1",
          "name": "Кабель U/UTP",
          "slug": "u-utp",
          "h1": "Кабель U/UTP",
          "description": null,
          "sortOrder": 20,
          "isVisibleInMenu": true,
          "children": []
        }
      ]
    }
  ]
}
```

DTO:

```text
PublicCategoryTreeResponse
- items: PublicCategoryTreeItemDto[]

PublicCategoryTreeItemDto
- id: uuid
- parentId: uuid | null
- name: string
- slug: string
- h1: string | null
- description: string | null
- sortOrder: number
- isVisibleInMenu: boolean
- children: PublicCategoryTreeItemDto[]
```

### GET `/api/public/catalog/categories/{slug}`

Возвращает публичную карточку активной категории.

Ответ:

```json
{
  "id": "6f830f45-0502-4cbf-8cda-f0ac8c74e7f1",
  "parentId": null,
  "name": "Витая пара",
  "slug": "vitaya-para",
  "description": "Кабель витая пара для СКС и сетевой инфраструктуры.",
  "h1": "Витая пара",
  "seo": {
    "title": "Витая пара купить",
    "description": "Каталог витой пары для сетей связи.",
    "canonicalPath": "/catalog/vitaya-para"
  },
  "breadcrumbs": [
    {
      "name": "Витая пара",
      "slug": "vitaya-para"
    }
  ]
}
```

DTO:

```text
PublicCategoryDetailDto
- id: uuid
- parentId: uuid | null
- name: string
- slug: string
- description: string | null
- h1: string | null
- seo: PublicSeoDto
- breadcrumbs: PublicBreadcrumbDto[]

PublicSeoDto
- title: string | null
- description: string | null
- canonicalPath: string

PublicBreadcrumbDto
- name: string
- slug: string
```

Ошибки:

- `404 catalog.category_not_found`, если категория отсутствует или неактивна.

### GET `/api/public/catalog/products`

Возвращает опубликованные товары каталога.

Параметры:

| Query | Тип | Обязателен | Правило |
| --- | --- | --- | --- |
| `categorySlug` | string | нет | Если указан, категория должна быть активной. |
| `page` | integer | нет | Минимум `1`, значение по умолчанию `1`. |
| `pageSize` | integer | нет | От `1` до `60`, значение по умолчанию `24`. |
| `sort` | string | нет | `category`, `name`, `newest`; значение по умолчанию `category`. |
| `brandSlug` | string | нет | Фильтр по активному бренду. |
| `availabilityStatus` | string | нет | Один из публичных кодов наличия. |
| `saleUnit` | string | нет | Один из публичных кодов единицы продажи. |
| `attribute.{code}` | string | нет | Значение фильтра по характеристике, на первой версии фильтрации используется slug option для `select`. |

Сортировки:

- `category`: `products.sort_order`, `products.name`, `products.slug`;
- `name`: `products.name`, `products.slug`;
- `newest`: `products.created_at desc`, `products.name`.

Ответ:

```json
{
  "items": [
    {
      "id": "e9c9e401-2f72-49a6-95bd-4e649cedeb3a",
      "name": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      "slug": "u-utp-cat-5e-cu-305m",
      "sku": "LC-UTP5E-CU-305",
      "brand": {
        "name": "LineCom",
        "slug": "linecom"
      },
      "category": {
        "name": "Витая пара",
        "slug": "vitaya-para"
      },
      "availability": {
        "code": "in_stock",
        "label": "В наличии"
      },
      "saleUnit": {
        "code": "coil",
        "label": "бухта"
      },
      "unitQuantity": "305 м",
      "mainImage": {
        "url": "/storage/products/u-utp-cat-5e-cu-305m.jpg",
        "alt": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
        "title": null
      }
    }
  ],
  "page": 1,
  "pageSize": 24,
  "totalItems": 1,
  "totalPages": 1
}
```

DTO:

```text
PublicProductListResponse
- items: PublicProductListItemDto[]
- page: number
- pageSize: number
- totalItems: number
- totalPages: number

PublicProductListItemDto
- id: uuid
- name: string
- slug: string
- sku: string | null
- brand: PublicBrandSummaryDto | null
- category: PublicCategorySummaryDto
- availability: PublicCodeLabelDto
- saleUnit: PublicCodeLabelDto
- unitQuantity: string
- mainImage: PublicImageDto | null

PublicBrandSummaryDto
- name: string
- slug: string

PublicCategorySummaryDto
- name: string
- slug: string

PublicCodeLabelDto
- code: string
- label: string

PublicImageDto
- url: string
- alt: string
- title: string | null
```

Ошибки:

- `400 catalog.invalid_pagination`;
- `400 catalog.invalid_sort`;
- `400 catalog.invalid_filter`;
- `404 catalog.category_not_found`, если `categorySlug` указан, но активная категория не найдена.

### GET `/api/public/catalog/products/{slug}`

Возвращает публичную карточку опубликованного товара.

Ответ:

```json
{
  "id": "e9c9e401-2f72-49a6-95bd-4e649cedeb3a",
  "name": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
  "slug": "u-utp-cat-5e-cu-305m",
  "sku": "LC-UTP5E-CU-305",
  "description": "Описание товара.",
  "shortDescription": "Кабель для структурированных кабельных систем.",
  "h1": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
  "category": {
    "name": "Витая пара",
    "slug": "vitaya-para"
  },
  "brand": {
    "name": "LineCom",
    "slug": "linecom"
  },
  "availability": {
    "code": "in_stock",
    "label": "В наличии"
  },
  "saleUnit": {
    "code": "coil",
    "label": "бухта"
  },
  "unitQuantity": "305 м",
  "images": [
    {
      "url": "/storage/products/u-utp-cat-5e-cu-305m.jpg",
      "alt": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      "title": null
    }
  ],
  "attributes": [
    {
      "code": "conductor-material",
      "name": "Материал проводника",
      "type": "select",
      "unit": null,
      "value": "CU",
      "sortOrder": 10
    }
  ],
  "seo": {
    "title": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
    "description": "Купить кабель U/UTP Cat 5e для СКС.",
    "canonicalPath": "/catalog/products/u-utp-cat-5e-cu-305m"
  },
  "breadcrumbs": [
    {
      "name": "Витая пара",
      "slug": "vitaya-para"
    },
    {
      "name": "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      "slug": "u-utp-cat-5e-cu-305m"
    }
  ]
}
```

DTO:

```text
PublicProductDetailDto
- id: uuid
- name: string
- slug: string
- sku: string | null
- description: string | null
- shortDescription: string | null
- h1: string | null
- category: PublicCategorySummaryDto
- brand: PublicBrandSummaryDto | null
- availability: PublicCodeLabelDto
- saleUnit: PublicCodeLabelDto
- unitQuantity: string
- images: PublicImageDto[]
- attributes: PublicProductAttributeDto[]
- seo: PublicSeoDto
- breadcrumbs: PublicBreadcrumbDto[]

PublicProductAttributeDto
- code: string
- name: string
- type: string
- unit: string | null
- value: string | number | boolean
- sortOrder: number
```

Ошибки:

- `404 catalog.product_not_found`, если товар отсутствует, не опубликован или его категория неактивна.

### GET `/api/public/catalog/categories/{slug}/filters`

Возвращает фильтры активной категории для публичного листинга товаров.

Ответ:

```json
{
  "category": {
    "name": "Витая пара",
    "slug": "vitaya-para"
  },
  "filters": [
    {
      "code": "conductor-material",
      "name": "Материал проводника",
      "type": "select",
      "unit": null,
      "sortOrder": 10,
      "options": [
        {
          "value": "CU",
          "slug": "cu",
          "sortOrder": 10
        }
      ]
    }
  ]
}
```

DTO:

```text
PublicCategoryFiltersDto
- category: PublicCategorySummaryDto
- filters: PublicFilterDto[]

PublicFilterDto
- code: string
- name: string
- type: string
- unit: string | null
- sortOrder: number
- options: PublicFilterOptionDto[]

PublicFilterOptionDto
- value: string
- slug: string
- sortOrder: number
```

Правила:

- В ответ попадают только активные и фильтруемые характеристики категории.
- Для `select` возвращаются только активные options.
- Для `boolean`, `number` и `text` поле `options` возвращается пустым массивом.
- Порядок фильтров: `sortOrder`, `name`, `code`.
- Порядок options: `sortOrder`, `value`, `slug`.

Ошибки:

- `404 catalog.category_not_found`, если категория отсутствует или неактивна.

## SEO/GEO правила API

- `slug` категорий и товаров является публичным URL-ключом.
- `seo.title`, `seo.description`, `h1` возвращаются в detail endpoints, чтобы frontend мог строить индексируемые страницы.
- `canonicalPath` возвращается как относительный публичный путь сайта без домена.
- В листинге товаров не возвращаются SEO-поля каждого товара, чтобы не раздувать ответ.
- Публичный API не создает индексируемые страницы для произвольных сочетаний фильтров. Индексация фильтров управляется отдельным контуром посадочных страниц.

## Границы первой реализации

В рамках этапа `Public Catalog read API` реализуются только публичные read-only endpoints каталога:

- категории;
- категория по slug;
- список товаров;
- карточка товара;
- фильтры категории.

В этот контракт не входят:

- публичные цены;
- онлайн-оплата;
- публичные остатки по складам;
- корзина и заявки;
- сравнение товаров как отдельный API;
- посадочные страницы как отдельный API;
- админские endpoints;
- auth, cookie и CSRF.

Эти контуры проектируются и реализуются отдельными этапами.
