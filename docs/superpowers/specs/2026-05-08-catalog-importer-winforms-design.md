# Catalog Importer WinForms Design

## Goal

Build the first production-oriented catalog import pipeline for LineCom, with a Windows desktop UI for controlled operator review.

The importer uses the normalized JSON produced from the 1C export:

- `Assets/1c_export_41_01_nomenclature_by_category.json`

The goal is not a disposable test seed. The goal is a repeatable import workflow that can fill the alpha catalog, let the customer evaluate the catalogization and display model, and later evolve toward a production import process.

## Product Intent

The alpha catalog should help validate:

- whether LineCom category grouping is understandable;
- whether product cards look useful without public prices;
- whether the request-based purchase model fits the customer;
- which product groups need manual review before publication;
- where attributes, images, and naming need further normalization.

The importer must not import public prices, cart semantics, online payment language, or order automation.

## Architecture

Use two layers:

- `CatalogImport.Core`: a UI-independent .NET library that reads source files, validates them, builds a preview plan, applies database changes, and writes reports.
- `CatalogImport.WinForms`: a Windows Forms desktop app that drives the import process and presents operator controls.

The core layer owns all import rules. The WinForms layer only handles file picking, preview tables, buttons, progress, and operator confirmation.

This keeps the import logic testable and reusable if a CLI, background worker, or web admin UI is added later.

## UI Choice

Use WinForms for the first desktop importer.

Reasons:

- the tool is Windows-only internal operations software;
- the first UI is mostly tables, forms, filters, progress, logs, and confirmation dialogs;
- WinForms provides a faster path to a practical desktop tool than WPF;
- rich styling is not a priority for this workflow.

WPF remains a future option only if the importer grows into a complex long-lived operations console with heavy custom visualization.

## First Workflow

The first WinForms version should be a wizard-like tool with these screens:

1. **Source selection**
   - Select normalized 1C JSON.
   - Select optional product image manifest.
   - Select target environment/connection string from safe local configuration.

2. **Dry-run preview**
   - Parse and validate source JSON.
   - Show category count, product count, high-confidence count, review-needed count, image-assignment count, and conflict count.
   - Do not write to the database.

3. **Review grid**
   - Show categories and products in filterable grids.
   - Highlight draft items, publishable items, missing images, duplicate slugs, duplicate external ids, and validation failures.
   - Allow copying/exporting rows for review.

4. **Import mode**
   - `Upsert only`: create or update catalog records without deleting existing catalog data.
   - `Reset catalog then import`: available only after explicit confirmation and safety checks.

5. **Apply**
   - Run the import in a database transaction where practical.
   - Show progress and current phase.
   - Stop on fatal validation/database errors.

6. **Report**
   - Write a machine-readable JSON report and a human-readable Markdown report.
   - Show imported, updated, skipped, failed, and warning counts.

## Import Rules

Categories:

- categories are upserted by `slug`;
- names come from source category names;
- categories are active and visible in menu by default for the alpha unless explicitly excluded by source rules;
- parent categories are not introduced in the first iteration unless the source JSON already contains a clear hierarchy.

Products:

- products are upserted by stable `external_id`;
- first iteration uses `1c:41.01:row:<sourceRow>` as the deterministic external id;
- product names come from source item names;
- slugs are deterministic and collision-safe;
- `sale_unit` and `unit_quantity` are derived conservatively from the product name and source data;
- `availability_status` defaults to `check_availability`;
- source quantity and accounting amounts are not exposed publicly.

Publication:

- items with `classification.confidence = high` and `needsReview = false` are eligible for `published`;
- other items are imported as `draft`;
- the dry-run report must show how many products will be published vs draft;
- the first implementation keeps this rule fixed; configurability can be added only in a later approved iteration.

Images:

- image assignment is a separate import phase after products are known;
- accepted images are read from `Assets/product-images/part1_png_reviewed_manifest.json` or a selected manifest with equivalent fields;
- only manifest items with `status = downloaded_png` and `visualReviewStatus = accepted_visual_scan` are attached automatically;
- failed image groups remain report warnings, not fatal product import errors;
- image files are registered in `stored_files` with `purpose = product_image`;
- `product_images` connects products to stored files through source rows.

Legal status:

- external image rights stay `requires-permission` in reports and documentation until a separate business decision changes this.

## Reset Safety

The reset mode must be explicit and guarded.

Before reset, the core must:

- count products, images, categories, and stored product-image files that would be affected;
- detect request items or other business records that may reference products;
- refuse destructive reset if protected references exist, unless a later approved design defines archival behavior.

The first version supports reset only for dev/QA environments. It must not provide a casual production wipe button.

## Database Boundary

The importer writes to existing catalog tables:

- `stored_files`;
- `categories`;
- `products`;
- `product_images`;
- optionally `brands` only if a reliable brand extraction rule is added in a later approved iteration.

The first version does not write:

- prices;
- public stock quantities;
- payments;
- orders;
- request history;
- attribute values unless a focused attribute-normalization design is approved separately.

## Error Handling

Validation errors must be split into:

- fatal errors: invalid JSON shape, unavailable database, schema mismatch, impossible required fields;
- row errors: product/category rows that cannot be imported;
- warnings: missing image, draft due to review-needed classification, weak metadata, non-core category.

Fatal errors stop apply. Row errors skip affected rows and are recorded. Warnings do not block import.

## Reports

Each dry-run and apply should write reports under an operator-selected output folder, defaulting to a local project artifacts folder.

Reports should include:

- source file paths and checksums;
- target database name/server where safe to record;
- import mode;
- start/end timestamps;
- category/product/image counts;
- created/updated/skipped/failed rows;
- slug and external-id conflicts;
- draft/published split;
- warnings and row errors.

## Testing

Core tests should cover:

- source JSON parsing;
- deterministic external id generation;
- deterministic and collision-safe slug generation;
- publish-vs-draft decision rules;
- dry-run report counts;
- reset safety refusal when protected references exist;
- image manifest mapping through `sourceRows`.

WinForms tests can stay light initially. The critical behavior belongs in `CatalogImport.Core`, where it can be unit-tested without UI automation.

## Verification For First Implementation

The first implementation should close only after:

- .NET build passes;
- existing API tests pass;
- new core unit tests pass;
- dry-run succeeds on `Assets/1c_export_41_01_nomenclature_by_category.json`;
- apply succeeds against a dev/QA database after explicit operator confirmation;
- public catalog endpoints show imported categories/products;
- frontend catalog pages render imported data;
- reports are saved and documented in `vault/Человекочитаемое`.

## Out Of Scope

- WPF implementation;
- web admin UI;
- automatic production scheduling;
- public prices;
- online payment;
- order automation;
- full 1C synchronization;
- automatic attribute extraction beyond fields needed for a useful alpha catalog;
- automatic legal approval of third-party product images.
