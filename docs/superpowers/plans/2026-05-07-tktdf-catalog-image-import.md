# TKTDF Catalog Image Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework the catalog image import pipeline to use `https://www.tktdf.ru/` as the trusted product-image source while keeping the LineCom frontend design unchanged.

**Architecture:** Keep local file storage as the target. Add a DbUp migration that lets one `stored_files` row be reused by multiple `product_images` assignments, then add a Python downloader that turns selected `tktdf.ru` product pages into PNG files plus a deterministic manifest. The downloader trusts the selected source page visually and only performs technical download/decode validation.

**Tech Stack:** PostgreSQL SQL migrations via DbUp, ASP.NET Core/xUnit migration text tests, Python 3 stdlib, Pillow, local filesystem assets.

---

## Source Context

- Design spec: `docs/superpowers/specs/2026-05-07-tktdf-catalog-image-import-design.md`
- Image iteration notes: `vault/Человекочитаемое/Catalog Image Import iterations.md`
- Catalog schema migration: `apps/dbmigrator/Migrations/002_catalog_foundation.sql`
- Migration tests: `tests/LineCom.Api.Tests/Infrastructure/Database/CatalogFoundationMigrationTests.cs`
- Existing image tools:
  - `tools/download_product_image_candidates.py`
  - `tools/download_product_png_review_batch.py`

Do not modify the LineCom frontend visual design in this plan.

## File Structure

- Create: `apps/dbmigrator/Migrations/005_product_image_shared_files.sql`
  - Removes the old unique index on `product_images(stored_file_id)`.
  - Adds unique cardinality for `(product_id, stored_file_id)`.
- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/ProductImageSharedFilesMigrationTests.cs`
  - Text-level regression tests for migration intent.
- Create: `tools/download_tktdf_product_images.py`
  - Trusted-source downloader for `tktdf.ru` product pages.
- Create: `tests/tools/test_download_tktdf_product_images.py`
  - Python unit tests for extraction, technical rejection, and manifest output.
- Create: `Assets/tktdf_image_sources_sample.json`
  - Small sample source file with a known `tktdf.ru` product page.
- Modify: `vault/Человекочитаемое/Catalog Image Import iterations.md`
  - Record the source switch, unchanged UI design, output paths, and verification results.

## Iteration Breakdown

Run these iterations one at a time. After each iteration, stop, review the result, and only then start the next one. If context is cleared, reopen this plan and start from the first iteration whose status is not recorded as complete in `vault/Человекочитаемое/Catalog Image Import iterations.md`.

### Iteration 1: Database Compatibility Only

Goal: make the catalog schema compatible with shared image files.

Do:

- Task 1 only.

Expected result:

- migration `005_product_image_shared_files.sql` exists;
- migration tests prove the old `stored_file_id` uniqueness is removed;
- `(product_id, stored_file_id)` is unique;
- the single-main-image rule remains.

Stop after:

```powershell
dotnet test LineCom.sln -m:1 --filter ProductImageSharedFilesMigrationTests
dotnet build LineCom.sln -m:1
```

Review focus:

- no frontend files changed;
- no importer behavior changed yet;
- migration is small and reversible by normal DbUp forward migration discipline.

### Iteration 2: TKTDF Downloader Sample

Goal: add the trusted-source downloader and prove it can produce a sample PNG manifest from `tktdf.ru`.

Do:

- Task 2 only.

Expected result:

- `tools/download_tktdf_product_images.py` exists;
- tests cover trusted source behavior, technical failure, and no token-match rejection;
- sample source and sample manifest exist;
- live one-item sample run downloads a PNG when network access is available.

Stop after:

```powershell
python -m unittest tests.tools.test_download_tktdf_product_images
python tools\download_tktdf_product_images.py --help
python tools\download_tktdf_product_images.py --source Assets\tktdf_image_sources_sample.json --output-dir Assets\product-images\tktdf_sample --manifest Assets\product-images\tktdf_sample_manifest.json --limit 1 --delay 0
```

Review focus:

- downloaded images are from `tktdf.ru`;
- manifest has `trusted_source_tktdf`;
- no prices, cart text, or external site UI data are imported;
- LineCom frontend design is untouched.

### Iteration 3: Documentation And Full Regression

Goal: document the new source and run full project verification.

Do:

- Task 3 only.

Expected result:

- `vault/Человекочитаемое/Catalog Image Import iterations.md` records the completed pass;
- .NET build/tests pass;
- Python tests pass;
- scope search does not reveal accidental forbidden commerce copy in implementation files.

Stop after:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
python -m unittest tests.tools.test_download_tktdf_product_images
python tools\download_tktdf_product_images.py --help
```

Review focus:

- exact commands and results are recorded in the vault note;
- remaining technical failures, if any, are concrete and not hidden;
- no intentional technical debt markers remain in changed implementation files.

## Resume Instructions After Context Cleanup

1. Read `AGENTS.md`.
2. Read `docs/superpowers/specs/2026-05-07-tktdf-catalog-image-import-design.md`.
3. Read this plan.
4. Read `vault/Человекочитаемое/Catalog Image Import iterations.md`.
5. Continue from the first incomplete iteration in the `Iteration Breakdown` section.
6. Do not start a later iteration until the user has reviewed the previous iteration result.

## Task 1: Database Compatibility For Shared Image Files

**Files:**

- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/ProductImageSharedFilesMigrationTests.cs`
- Create: `apps/dbmigrator/Migrations/005_product_image_shared_files.sql`

- [ ] **Step 1: Write the failing migration test**

Create `tests/LineCom.Api.Tests/Infrastructure/Database/ProductImageSharedFilesMigrationTests.cs`:

```csharp
namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class ProductImageSharedFilesMigrationTests
{
    private static readonly string MigrationSql = ReadMigration("005_product_image_shared_files.sql");

    [Fact]
    public void ProductImageSharedFiles_RemovesStoredFileUniqueness()
    {
        Assert.Contains("DROP INDEX IF EXISTS ux_product_images_stored_file_id;", MigrationSql);
    }

    [Fact]
    public void ProductImageSharedFiles_AddsProductFilePairUniqueness()
    {
        var normalizedSql = MigrationSql.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains(
            "CREATE UNIQUE INDEX IF NOT EXISTS ux_product_images_product_id_stored_file_id\n    ON product_images (product_id, stored_file_id);",
            normalizedSql);
    }

    [Fact]
    public void ProductImageSharedFiles_DoesNotRemoveSingleMainImageRule()
    {
        Assert.DoesNotContain("DROP INDEX IF EXISTS ux_product_images_single_main", MigrationSql);
        Assert.DoesNotContain("DROP INDEX ux_product_images_single_main", MigrationSql);
    }

    private static string ReadMigration(string fileName)
    {
        var migrationFile = Path.Combine(FindRepositoryRoot(), "apps", "dbmigrator", "Migrations", fileName);

        return File.ReadAllText(migrationFile);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run from repository root:

```powershell
dotnet test LineCom.sln -m:1 --filter ProductImageSharedFilesMigrationTests
```

Expected: fail because `005_product_image_shared_files.sql` does not exist.

- [ ] **Step 3: Add the DbUp migration**

Create `apps/dbmigrator/Migrations/005_product_image_shared_files.sql`:

```sql
DROP INDEX IF EXISTS ux_product_images_stored_file_id;

CREATE UNIQUE INDEX IF NOT EXISTS ux_product_images_product_id_stored_file_id
    ON product_images (product_id, stored_file_id);
```

- [ ] **Step 4: Run the migration test and build**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter ProductImageSharedFilesMigrationTests
dotnet build LineCom.sln -m:1
```

Expected:

- migration test passes;
- build passes, allowing existing `NU1900` warnings only if NuGet vulnerability feed is unavailable.

- [ ] **Step 5: Commit database compatibility**

Run:

```powershell
git add apps/dbmigrator/Migrations/005_product_image_shared_files.sql tests/LineCom.Api.Tests/Infrastructure/Database/ProductImageSharedFilesMigrationTests.cs
git commit -m "feat: allow shared product image files"
```

## Task 2: Trusted TKTDF Downloader

**Files:**

- Create: `tests/tools/test_download_tktdf_product_images.py`
- Create: `tools/download_tktdf_product_images.py`
- Create: `Assets/tktdf_image_sources_sample.json`

- [ ] **Step 1: Write failing Python tests**

Create `tests/tools/test_download_tktdf_product_images.py`:

```python
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from PIL import Image

from tools import download_tktdf_product_images as downloader


def png_bytes() -> bytes:
    with tempfile.NamedTemporaryFile(suffix=".png") as file:
        Image.new("RGB", (24, 16), "white").save(file.name, "PNG")
        return Path(file.name).read_bytes()


class TktdfDownloaderTests(unittest.TestCase):
    def test_downloads_trusted_tktdf_image_to_png_manifest(self) -> None:
        html = """
        <html>
          <body>
            <h1>Кабель U/UTP4 cat.5e</h1>
            <span>Код: 51108</span>
            <img src="/upload/iblock/abc/product.jpg" alt="Кабель UTP">
          </body>
        </html>
        """
        responses = {
            "https://www.tktdf.ru/catalog/id/51108/": (html.encode("utf-8"), "text/html"),
            "https://www.tktdf.ru/upload/iblock/abc/product.jpg": (png_bytes(), "image/png"),
        }

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "source.json"
            output_dir = root / "images"
            manifest = root / "manifest.json"
            source.write_text(
                json.dumps(
                    {
                        "items": [
                            {
                                "assetKey": "netko-utp-cat5e",
                                "sourceRows": [1, 2],
                                "sourceProductUrl": "https://www.tktdf.ru/catalog/id/51108/",
                                "isMain": True,
                            }
                        ]
                    },
                    ensure_ascii=False,
                ),
                encoding="utf-8",
            )

            with patch.object(downloader, "fetch", side_effect=lambda url: responses[url]):
                exit_code = downloader.run(
                    source_path=source,
                    output_dir=output_dir,
                    manifest_path=manifest,
                    limit=0,
                    delay=0,
                )

            self.assertEqual(0, exit_code)
            data = json.loads(manifest.read_text(encoding="utf-8"))
            self.assertEqual(1, data["downloadedPng"])
            item = data["items"][0]
            self.assertEqual("downloaded_png", item["status"])
            self.assertEqual("tktdf.ru", item["sourceSite"])
            self.assertEqual("51108", item["sourceProductCode"])
            self.assertEqual("trusted_source_tktdf", item["visualReviewStatus"])
            self.assertEqual("requires-permission", item["rightsStatus"])
            self.assertEqual("https://www.tktdf.ru/upload/iblock/abc/product.jpg", item["sourceImageUrl"])
            self.assertTrue((root / item["file"]).exists())

    def test_broken_image_is_recorded_as_technical_failure(self) -> None:
        html = '<html><body><span>Код: 51108</span><img src="/upload/iblock/abc/product.jpg"></body></html>'
        responses = {
            "https://www.tktdf.ru/catalog/id/51108/": (html.encode("utf-8"), "text/html"),
            "https://www.tktdf.ru/upload/iblock/abc/product.jpg": (b"not image", "image/jpeg"),
        }

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "source.json"
            output_dir = root / "images"
            manifest = root / "manifest.json"
            source.write_text(
                json.dumps(
                    {
                        "items": [
                            {
                                "assetKey": "broken-image",
                                "sourceRows": [1],
                                "sourceProductUrl": "https://www.tktdf.ru/catalog/id/51108/",
                            }
                        ]
                    }
                ),
                encoding="utf-8",
            )

            with patch.object(downloader, "fetch", side_effect=lambda url: responses[url]):
                exit_code = downloader.run(
                    source_path=source,
                    output_dir=output_dir,
                    manifest_path=manifest,
                    limit=0,
                    delay=0,
                )

            self.assertEqual(1, exit_code)
            data = json.loads(manifest.read_text(encoding="utf-8"))
            self.assertEqual(0, data["downloadedPng"])
            self.assertEqual("failed", data["items"][0]["status"])
            self.assertIn("attempts", data["items"][0])

    def test_extract_image_urls_does_not_require_product_token_match(self) -> None:
        html = '<html><body><img src="/upload/iblock/zzz/unrelated-name.jpg"></body></html>'

        urls = downloader.extract_image_urls(html, "https://www.tktdf.ru/catalog/id/1/")

        self.assertEqual(["https://www.tktdf.ru/upload/iblock/zzz/unrelated-name.jpg"], urls)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
python -m unittest tests.tools.test_download_tktdf_product_images
```

Expected: fail because `tools/download_tktdf_product_images.py` does not exist.

- [ ] **Step 3: Add the trusted-source downloader**

Create `tools/download_tktdf_product_images.py`:

```python
import argparse
import hashlib
import json
import re
import time
from html import unescape
from io import BytesIO
from pathlib import Path
from urllib.parse import urljoin, urlparse
from urllib.request import Request, urlopen

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "Assets" / "tktdf_image_sources_sample.json"
DEFAULT_OUTPUT_DIR = ROOT / "Assets" / "product-images" / "tktdf"
DEFAULT_MANIFEST = ROOT / "Assets" / "product-images" / "tktdf_manifest.json"

USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) "
    "Chrome/124.0 Safari/537.36"
)

IMAGE_ATTRS = (
    "src",
    "data-src",
    "data-original",
    "data-lazy-src",
    "data-url",
    "data-image",
    "data-large",
    "data-zoom-image",
)


def fetch(url: str, timeout: int = 25) -> tuple[bytes, str]:
    request = Request(url, headers={"User-Agent": USER_AGENT, "Accept": "*/*"})
    with urlopen(request, timeout=timeout) as response:
        return response.read(), response.headers.get("content-type", "")


def slug(value: str) -> str:
    value = value.lower().strip()
    value = re.sub(r"[^a-z0-9._-]+", "-", value)
    return re.sub(r"-+", "-", value).strip("-") or "image"


def is_tktdf_url(url: str) -> bool:
    host = urlparse(url).netloc.lower()
    return host in {"www.tktdf.ru", "tktdf.ru"}


def extract_product_code(html: str) -> str | None:
    match = re.search(r"Код:\s*([0-9A-Za-zА-Яа-я_-]+)", html)
    return match.group(1) if match else None


def extract_image_urls(html: str, page_url: str) -> list[str]:
    urls: list[str] = []

    meta_pattern = re.compile(
        r"<meta\b[^>]*(?:property|name)=[\"'](?:og:image|twitter:image)[\"'][^>]*>",
        re.I,
    )
    content_pattern = re.compile(r"\bcontent=[\"']([^\"']+)[\"']", re.I)
    for tag in meta_pattern.findall(html):
        match = content_pattern.search(tag)
        if match:
            urls.append(urljoin(page_url, unescape(match.group(1))))

    img_pattern = re.compile(r"<img\b[^>]*>", re.I)
    attr_pattern = re.compile(r"([a-zA-Z_:.-]+)\s*=\s*([\"'])(.*?)\2", re.S)
    for tag in img_pattern.findall(html):
        attrs = {name.lower(): unescape(value.strip()) for name, _, value in attr_pattern.findall(tag)}
        for attr in IMAGE_ATTRS:
            if attrs.get(attr):
                urls.append(urljoin(page_url, attrs[attr]))
        srcset = attrs.get("srcset") or attrs.get("data-srcset")
        if srcset:
            urls.extend(urljoin(page_url, part.strip().split(" ")[0]) for part in srcset.split(",") if part.strip())

    unique_urls: list[str] = []
    seen = set()
    for url in urls:
        if url in seen:
            continue
        seen.add(url)
        lower_url = url.lower()
        if not is_tktdf_url(url):
            continue
        if "/upload/iblock/" not in lower_url and "/upload/resize_cache/" not in lower_url:
            continue
        unique_urls.append(url)

    return unique_urls


def decode_image(body: bytes) -> Image.Image:
    image = Image.open(BytesIO(body))
    image.load()
    if image.width <= 0 or image.height <= 0:
        raise ValueError("image has empty dimensions")
    if image.mode not in {"RGB", "RGBA"}:
        image = image.convert("RGBA" if "A" in image.mode else "RGB")
    return image


def relative_to_root(path: Path) -> str:
    try:
        return str(path.relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        return str(path).replace("\\", "/")


def download_one(item: dict, output_dir: Path) -> dict:
    asset_key = item["assetKey"]
    source_product_url = item["sourceProductUrl"]
    attempts = []

    try:
        page_body, page_content_type = fetch(source_product_url)
        if "html" not in page_content_type.lower() and not page_body.lstrip().startswith(b"<"):
            candidate_urls = [source_product_url]
            html = ""
        else:
            html = page_body.decode("utf-8", errors="replace")
            candidate_urls = extract_image_urls(html, source_product_url)
    except Exception as exc:
        return {
            "assetKey": asset_key,
            "sourceSite": "tktdf.ru",
            "sourceRows": item.get("sourceRows", []),
            "sourceProductUrl": source_product_url,
            "status": "failed",
            "attempts": [{"status": "page_failed", "error": str(exc)}],
        }

    product_code = extract_product_code(html) if html else None
    for image_url in candidate_urls:
        try:
            body, content_type = fetch(image_url)
            checksum = hashlib.sha256(body).hexdigest()
            image = decode_image(body)
            target = output_dir / f"{slug(asset_key)}.png"
            output_dir.mkdir(parents=True, exist_ok=True)
            image.convert("RGBA").save(target, "PNG", optimize=True)

            return {
                "assetKey": asset_key,
                "sourceSite": "tktdf.ru",
                "sourceRows": item.get("sourceRows", []),
                "sourceProductUrl": source_product_url,
                "sourceProductCode": product_code,
                "sourceImageUrl": image_url,
                "status": "downloaded_png",
                "file": relative_to_root(target),
                "width": image.width,
                "height": image.height,
                "checksum": checksum,
                "contentType": content_type,
                "isMain": bool(item.get("isMain", True)),
                "visualReviewStatus": "trusted_source_tktdf",
                "rightsStatus": item.get("rightsStatus", "requires-permission"),
            }
        except Exception as exc:
            attempts.append({"sourceImageUrl": image_url, "status": "image_failed", "error": str(exc)})

    return {
        "assetKey": asset_key,
        "sourceSite": "tktdf.ru",
        "sourceRows": item.get("sourceRows", []),
        "sourceProductUrl": source_product_url,
        "sourceProductCode": product_code,
        "status": "failed",
        "attempts": attempts[-12:],
    }


def read_items(source_path: Path) -> list[dict]:
    data = json.loads(source_path.read_text(encoding="utf-8"))
    if isinstance(data.get("items"), list):
        return data["items"]
    raise ValueError("Source JSON must contain an items array.")


def run(source_path: Path, output_dir: Path, manifest_path: Path, limit: int, delay: float) -> int:
    items = read_items(source_path)
    if limit:
        items = items[:limit]

    results = []
    for item in items:
        result = download_one(item, output_dir)
        results.append(result)
        print(f"{result['status']}: {item['assetKey']}")
        if delay:
            time.sleep(delay)

    downloaded = [item for item in results if item["status"] == "downloaded_png"]
    manifest = {
        "source": relative_to_root(source_path),
        "sourceSite": "tktdf.ru",
        "outputDir": relative_to_root(output_dir),
        "totalItemsAttempted": len(results),
        "downloadedPng": len(downloaded),
        "failed": sum(1 for item in results if item["status"] != "downloaded_png"),
        "items": results,
    }
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0 if downloaded else 1


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--delay", type=float, default=0.3)
    args = parser.parse_args()

    return run(
        source_path=args.source.resolve(),
        output_dir=args.output_dir.resolve(),
        manifest_path=args.manifest.resolve(),
        limit=args.limit,
        delay=args.delay,
    )


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 4: Add the sample source file**

Create `Assets/tktdf_image_sources_sample.json`:

```json
{
  "batch": {
    "sourceSite": "tktdf.ru",
    "createdAt": "2026-05-07",
    "notes": "Sample trusted-source product image import input. LineCom frontend design stays unchanged."
  },
  "items": [
    {
      "assetKey": "tktdf-netko-utp-cat5e-51108",
      "sourceRows": [],
      "sourceProductUrl": "https://www.tktdf.ru/catalog/id/51108/",
      "isMain": true,
      "rightsStatus": "requires-permission"
    }
  ]
}
```

- [ ] **Step 5: Run Python tests**

Run:

```powershell
python -m unittest tests.tools.test_download_tktdf_product_images
```

Expected: all tests pass.

- [ ] **Step 6: Run downloader help and a one-item live sample**

Run:

```powershell
python tools\download_tktdf_product_images.py --help
python tools\download_tktdf_product_images.py --source Assets\tktdf_image_sources_sample.json --output-dir Assets\product-images\tktdf_sample --manifest Assets\product-images\tktdf_sample_manifest.json --limit 1 --delay 0
```

Expected:

- `--help` prints arguments;
- sample run creates `Assets/product-images/tktdf_sample_manifest.json`;
- manifest has `downloadedPng` at least `1`;
- output image is PNG.

If the live sample fails due to network sandboxing, rerun the same command with escalation and approval.

- [ ] **Step 7: Commit downloader**

Run:

```powershell
git add tools/download_tktdf_product_images.py tests/tools/test_download_tktdf_product_images.py Assets/tktdf_image_sources_sample.json Assets/product-images/tktdf_sample Assets/product-images/tktdf_sample_manifest.json
git commit -m "feat: add trusted tktdf image downloader"
```

## Task 3: Documentation And Full Verification

**Files:**

- Modify: `vault/Человекочитаемое/Catalog Image Import iterations.md`

- [ ] **Step 1: Update iteration notes**

Append a new section to `vault/Человекочитаемое/Catalog Image Import iterations.md`:

```markdown
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
- downloader: `tools/download_tktdf_product_images.py`;
- sample source: `Assets/tktdf_image_sources_sample.json`;
- sample manifest: `Assets/product-images/tktdf_sample_manifest.json`.

Проверки:

- `dotnet build LineCom.sln -m:1`;
- `dotnet test LineCom.sln -m:1`;
- `python -m unittest tests.tools.test_download_tktdf_product_images`;
- `python tools\download_tktdf_product_images.py --help`;
- `python tools\download_tktdf_product_images.py --source Assets\tktdf_image_sources_sample.json --output-dir Assets\product-images\tktdf_sample --manifest Assets\product-images\tktdf_sample_manifest.json --limit 1 --delay 0`.
```

- [ ] **Step 2: Run full verification**

Run:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
python -m unittest tests.tools.test_download_tktdf_product_images
python tools\download_tktdf_product_images.py --help
```

Expected:

- .NET build passes;
- .NET tests pass;
- Python unit tests pass;
- downloader help prints successfully.

- [ ] **Step 3: Search for accidental forbidden scope**

Run:

```powershell
rg -n "Купить|В корзину|Розничная цена|Мелкий опт|оплат|TODO|TBD|FIXME|заглуш|костыл" tools tests apps/dbmigrator docs/superpowers/specs docs/superpowers/plans vault/Человекочитаемое
```

Expected:

- no forbidden commerce language in importer implementation;
- documentation matches are only explicit excluded-scope notes or historical notes;
- no unfinished markers in changed implementation files.

- [ ] **Step 4: Commit documentation update**

Run:

```powershell
git add vault/Человекочитаемое/Catalog Image Import iterations.md
git commit -m "docs: record tktdf image import iteration"
```

## Self-Review

Spec coverage:

- Trusted `tktdf.ru` source: Task 2.
- LineCom design unchanged: source context, Task 3 notes, excluded scope.
- Technical validation without visual matching: Task 2 tests and downloader behavior.
- Shared image-file DB model: Task 1.
- Local file storage and PNG output: Task 2.
- Documentation of output paths and verification: Task 3.

No placeholders are intentionally left. The implementation does not import prices, cart behavior, payment language, or frontend design from `tktdf.ru`.
