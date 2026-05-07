# TKTDF Catalog Image Import Design

## Goal

Build the next catalog image import iteration around `https://www.tktdf.ru/` as the trusted product-image source.
The LineCom site design, public catalog UI, request flow, and visual style stay unchanged. This iteration changes only the image acquisition, manifest, database compatibility, and import path for product images.

## Source Decision

`tktdf.ru` is the preferred image source for this iteration.

The importer should not perform semantic visual matching against multiple external sites. Images from `tktdf.ru` are accepted as the desired product visuals when the source product page is selected for an import item.

The importer still performs technical validation:

- the page or image URL is reachable;
- the response is an image or contains discoverable product image URLs;
- the image can be decoded;
- the image has non-zero dimensions;
- the output file can be written as PNG;
- a checksum is recorded.

These checks protect the pipeline from broken downloads and HTML/error pages. They are not visual correspondence checks.

## Scope

Included:

- trusted-source image downloader for `tktdf.ru`;
- output folder under `Assets/product-images/tktdf/`;
- manifest with source product URLs, source image URLs, product codes when available, checksums, and trusted-source review status;
- database migration that allows one stored file to be attached to several products;
- tests for migration text and importer behavior;
- documentation update in `vault/Человекочитаемое/Catalog Image Import iterations.md`.

Excluded:

- changing the LineCom frontend design;
- replacing public catalog copy, colors, layout, or interaction model;
- importing prices, cart behavior, or commercial wording from `tktdf.ru`;
- adding online payment or order semantics;
- changing product matching rules for the main 1C catalog import;
- publishing unapproved legal/commercial usage guarantees for third-party images.

## Data Model

Current schema has `product_images.stored_file_id` unique. That blocks one physical image file from being reused by multiple LineCom products.

The target model:

- `stored_files` represents a physical local file;
- `product_images` represents a product-to-file image assignment;
- the same `stored_file_id` may appear in multiple `product_images` rows;
- the same `(product_id, stored_file_id)` pair must not be duplicated;
- each product still has at most one main image through the existing `ux_product_images_single_main` partial index.

Migration intent:

```sql
DROP INDEX IF EXISTS ux_product_images_stored_file_id;

CREATE UNIQUE INDEX ux_product_images_product_id_stored_file_id
    ON product_images (product_id, stored_file_id);
```

The existing `validate_product_image_file()` trigger remains valid and continues to enforce `stored_files.purpose = 'product_image'`.

## Import Manifest

The new manifest format is product-source oriented. Each accepted item records where the image came from and what file was produced.

Required item fields:

- `sourceSite`: `tktdf.ru`;
- `sourceProductUrl`;
- `sourceProductCode` when the page exposes a code;
- `sourceImageUrl`;
- `file`;
- `assetKey`;
- `sourceRows`;
- `width`;
- `height`;
- `checksum`;
- `contentType`;
- `visualReviewStatus`: `trusted_source_tktdf`;
- `rightsStatus`: `requires-permission` unless a separate business decision records another status.

Failed items stay in the manifest with:

- `assetKey`;
- `sourceProductUrl` when known;
- `status`;
- recent technical errors.

## Downloader Behavior

The downloader accepts a source JSON file with image groups and `tktdf.ru` product page URLs. It should support an explicit limit for safe test runs.

For each item:

1. Fetch the product page.
2. Extract product code if present, for example text near `Код: 51108`.
3. Extract product image candidates from direct image links, `img` attributes, gallery links, and metadata.
4. Prefer images from `www.tktdf.ru` or relative `/upload/iblock/...` URLs.
5. Download image candidates until one or more valid images are decoded.
6. Convert accepted files to PNG.
7. Write a deterministic file name under `Assets/product-images/tktdf/`.
8. Write or update a manifest.

No visual rejection rules should reject a valid `tktdf.ru` product image because of low variance, ratio, missing alt text, or product-token mismatch.

## Database Import Boundary

This iteration may prepare files and manifest and may make the database schema compatible with shared image assets.

Actual insertion of `stored_files` and `product_images` should be a separate explicit step or script with a dry-run mode. The script must not silently overwrite existing product images. If a product already has a main image, the script should report it and skip unless an explicit replace flag is passed.

## Tests

Backend migration tests should assert:

- the old unique index on `stored_file_id` is removed in the new migration;
- the new unique index on `(product_id, stored_file_id)` exists;
- the single-main-image constraint remains intact.

Importer tests should assert:

- a trusted `tktdf.ru` page with a product code and `/upload/iblock/...jpg` image produces a PNG manifest item;
- the importer records `trusted_source_tktdf`;
- broken image responses fail as technical errors;
- no visual token-matching rejection is applied.

## Verification

Required checks before closing the iteration:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
python tools\download_tktdf_product_images.py --help
python tools\download_tktdf_product_images.py --source Assets\tktdf_image_sources_sample.json --output-dir Assets\product-images\tktdf_sample --manifest Assets\product-images\tktdf_sample_manifest.json --limit 1
```

If the downloader needs network access in the sandbox and fails due to network restrictions, rerun with approval rather than weakening the importer.

## Documentation Notes

Update `vault/Человекочитаемое/Catalog Image Import iterations.md` with:

- `tktdf.ru` as the trusted source for the next pass;
- the fact that LineCom frontend design stays unchanged;
- the new DB sharing rule for `stored_files`;
- the exact output folder and manifest path used by the run;
- any failed product pages or technical download failures.

## Open Decisions Resolved

- Source: `https://www.tktdf.ru/`.
- Visual matching: skipped for this trusted source.
- LineCom UI design: unchanged.
- File storage target: local file storage.
- Image output format: PNG.
- Legal status default: `requires-permission` until separately changed.
