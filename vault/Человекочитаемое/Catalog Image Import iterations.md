# Catalog Image Import iterations

## 2026-05-07. Первая проверенная партия изображений

Цель итерации: подготовить изображения для части номенклатуры из `41.01` без прежней ошибки `1 товар = 1 картинка`. Используется модель `imageAsset` как визуальная группа, которая может покрывать несколько строк номенклатуры.

### Зафиксированные артефакты

- Исходная классификация номенклатуры: `Assets/1c_export_41_01_nomenclature_by_category.json`.
- Кандидаты источников изображений и привязки к строкам 1С: `Assets/product_image_candidates_part1.json`.
- Проверенные PNG: `Assets/product-images/part1_png_reviewed/`.
- Манифест скачивания и визуальной проверки: `Assets/product-images/part1_png_reviewed_manifest.json`.
- Контакт-лист для ручной визуальной проверки: `Assets/product-images/part1_png_reviewed_contact_sheet.png`.
- Скрипт первичного поиска/скачивания кандидатов: `tools/download_product_image_candidates.py`.
- Актуальный строгий скрипт скачивания PNG с контакт-листом: `tools/download_product_png_review_batch.py`.

### Текущий результат

- Попытка скачивания выполнена для 17 высокоуверенных image-групп без обязательного операторского ревью.
- Скачано и сохранено в PNG: 14 файлов.
- Визуально проверено по контакт-листу: 14 файлов.
- Не скачаны и не заменены случайными картинками: 3 группы.

Не скачанные группы:

- `sfp-dac-2m`.
- `fiber-patchcord-sc-upc-sc-upc-simplex`.
- `optical-adapter-sc-apc-simplex`.

### Обязательный алгоритм следующих проходов

1. Формировать `imageAssets`, а не отдельное изображение на каждую строку товара.
2. Для похожих товаров использовать общий `assetKey` и привязку через `productImageAssignments.sourceRows`.
3. Скачивать только в отдельную новую папку итерации, например `Assets/product-images/part2_png_reviewed/`.
4. Итоговые файлы должны быть только `.png`.
5. После скачивания обязательно строить contact sheet.
6. Проверять contact sheet глазами до принятия партии.
7. Оставлять в итоговой папке только изображения, реально соответствующие товарной группе.
8. Если источник отдает `403`, документ, логотип, сертификат, заглушку или близкий, но неточный товар, группа остается `failed`; случайные подмены запрещены.
9. В манифесте для принятых файлов выставлять `visualReviewStatus: accepted_visual_scan`.
10. Всегда сохранять `sourcePageUrl`, `imageUrl`, `sourceRows`, `rightsStatus` и `matchConfidence`.

### Правила качества картинок

- Формат: только PNG.
- Изображение должно показывать сам товар или визуально эквивалентную товарную группу.
- Для длин кабелей, процентов PLC-делителей и портности кроссов допускается одна общая картинка, если внешний вид товара в карточке магазина не вводит покупателя в заблуждение.
- Не использовать сертификаты, документы, логотипы, баннеры, иконки, заглушки, изображения упаковки без товара.
- Все внешние изображения имеют `rightsStatus: requires-permission`, пока нет договора/разрешения поставщика или замены на собственные фото.

### Команда для повторения строгого прохода

```powershell
python tools\download_product_png_review_batch.py `
  --source Assets\product_image_candidates_part1.json `
  --output-dir Assets\product-images\part1_png_reviewed `
  --manifest Assets\product-images\part1_png_reviewed_manifest.json `
  --contact-sheet Assets\product-images\part1_png_reviewed_contact_sheet.png
```

Для тестового запуска перед массовой загрузкой:

```powershell
python tools\download_product_png_review_batch.py `
  --source Assets\product_image_candidates_part1.json `
  --output-dir Assets\product-images\partN_png_reviewed `
  --manifest Assets\product-images\partN_png_reviewed_manifest.json `
  --contact-sheet Assets\product-images\partN_png_reviewed_contact_sheet.png `
  --limit 8
```

### Дальнейшая загрузка в БД

Готовить импорт нужно через нормализованную структуру:

- `imageAssets` как уникальные файлы/визуальные группы.
- `productImageAssignments` как привязки этих файлов к товарам.

Перед реальной миграцией схемы нужно учесть, что текущий индекс `ux_product_images_stored_file_id` не позволяет одному `stored_file_id` быть привязанным к нескольким товарам. Для общего изображения на несколько товаров потребуется убрать эту уникальность или ввести отдельную сущность визуальных групп/ассетов.

## 2026-05-07. Переход на trusted-source импорт изображений с tktdf.ru

Цель итерации: заменить прежний multi-source поиск картинок на доверенный источник `https://www.tktdf.ru/`.
Дизайн сайта LineCom не меняется: публичный каталог, карточки, цвета, layout и заявочная модель остаются прежними.

Решения:

- `tktdf.ru` используется как доверенный источник товарных изображений.
- Визуальная проверка соответствия для этого источника не выполняется.
- Технические проверки скачивания, декодирования, размеров и checksum остаются обязательными.
- Цены, корзина, тексты покупки и коммерческие механики с `tktdf.ru` не импортируются.
- `stored_files` теперь может переиспользоваться несколькими товарами через разные `product_images`.

Артефакты:

- spec: `docs/superpowers/specs/2026-05-07-tktdf-catalog-image-import-design.md`;
- plan: `docs/superpowers/plans/2026-05-07-tktdf-catalog-image-import.md`;
- migration: `apps/dbmigrator/Migrations/005_product_image_shared_files.sql`;
- migration tests: `tests/LineCom.Api.Tests/Infrastructure/Database/ProductImageSharedFilesMigrationTests.cs`;
- downloader: `tools/download_tktdf_product_images.py`;
- downloader tests: `tests/tools/test_download_tktdf_product_images.py`;
- sample source: `Assets/tktdf_image_sources_sample.json`;
- sample output folder: `Assets/product-images/tktdf_sample/`;
- sample manifest: `Assets/product-images/tktdf_sample_manifest.json`.

Проверки:

- `dotnet build LineCom.sln -m:1`;
- `dotnet test LineCom.sln -m:1`;
- `python -m unittest tests.tools.test_download_tktdf_product_images`;
- `python tools\download_tktdf_product_images.py --help`;
- `python tools\download_tktdf_product_images.py --source Assets\tktdf_image_sources_sample.json --output-dir Assets\product-images\tktdf_sample --manifest Assets\product-images\tktdf_sample_manifest.json --limit 1 --delay 0`.

Результаты проверки:

- `dotnet build LineCom.sln -m:1`: успешно, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test LineCom.sln -m:1`: успешно, `284` passed, `0` failed, `0` skipped.
- `python -m unittest tests.tools.test_download_tktdf_product_images`: успешно, `Ran 3 tests`, `OK`.
- `python tools\download_tktdf_product_images.py --help`: успешно, CLI выводит `--source`, `--output-dir`, `--manifest`, `--limit`, `--delay`.
- `python tools\download_tktdf_product_images.py --source Assets\tktdf_image_sources_sample.json --output-dir Assets\product-images\tktdf_sample --manifest Assets\product-images\tktdf_sample_manifest.json --limit 1 --delay 0`: успешно, `downloaded_png: tktdf-netko-utp-cat5e-51108`.

Scope-search:

- Команда `rg -n "Купить|В корзину|Розничная цена|Мелкий опт|оплат|TODO|TBD|FIXME|заглуш|костыл" tools tests apps/dbmigrator docs/superpowers/specs docs/superpowers/plans vault/Человекочитаемое` не выявила запрещенный коммерческий текст или незакрытые TODO/FIXME в новых importer/migration implementation files.
- Найденные совпадения относятся к существующим тестам публичного каталога, историческим/архитектурным документам, excluded-scope формулировкам и самим правилам проверки.
