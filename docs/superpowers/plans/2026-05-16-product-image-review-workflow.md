# Product Image Review Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a category-by-category image review workflow that gathers product image candidates, shows them in a local Playwright-friendly browser review UI, writes the operator selection, downloads selected images to PNG, and applies them to LineCom Local FileStorage only after dry-run confirmation.

**Architecture:** Keep discovery/review/finalization in Python under `tools/` because existing image tooling is Python and easy to run outside the web app. Add a focused C# import service in `LineCom.CatalogImport.Core` for the final database/storage apply step, instead of pushing this through the public admin UI. Store operator-review artifacts under `Assets/product-image-review/<category-slug>/` and final PNG/manifest output under `Assets/product-images/reviewed/<category-slug>/`.

**Tech Stack:** Python standard library, Pillow, optional Playwright Python for browser QA, Dapper/Npgsql in `LineCom.CatalogImport.Core`, DbUp-managed PostgreSQL schema already present.

---

## File Structure

- Create `tools/product_image_review_workflow.py`: CLI entrypoint and pure helpers for candidate JSON, selection JSON, HTML review generation, and selected-image finalization.
- Create `tests/tools/test_product_image_review_workflow.py`: unit tests for selection limits, manifest output, filtering, HTML content, and mocked downloads.
- Create `apps/catalog-import.core/Images/ReviewedProductImageManifestReader.cs`: typed reader for reviewed manifest entries.
- Create `apps/catalog-import.core/Images/ReviewedProductImageApplyService.cs`: dry-run/apply service that inserts `stored_files` and `product_images` for reviewed images.
- Modify `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`: include any new compile items automatically if SDK-style project already does so; only touch if explicit includes exist.
- Create `tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageManifestReaderTests.cs`: manifest reader tests.
- Create `tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageApplySqlTests.cs`: SQL text and conservative behavior tests.
- Modify `vault/Человекочитаемое/Catalog Image Import iterations.md`: record the new review workflow after implementation.

## Task 1: Python Review Models And Selection Rules

**Files:**
- Create: `tools/product_image_review_workflow.py`
- Create: `tests/tools/test_product_image_review_workflow.py`

- [ ] **Step 1: Write failing tests for selection normalization**

Add tests that define the expected JSON shape and enforce at most two selected candidates per product:

```python
import json
import tempfile
import unittest
from pathlib import Path

from tools import product_image_review_workflow as workflow


class ProductImageReviewWorkflowTests(unittest.TestCase):
    def test_normalize_selection_keeps_first_two_selected_per_product(self) -> None:
        candidates = {
            "category": {"slug": "cable", "name": "Кабель"},
            "products": [
                {
                    "productId": "p1",
                    "externalId": "101",
                    "name": "Кабель UTP",
                    "candidates": [
                        {"candidateId": "a", "selected": True, "sourceSite": "tktdf.ru"},
                        {"candidateId": "b", "selected": True, "sourceSite": "redmrt.ru"},
                        {"candidateId": "c", "selected": True, "sourceSite": "google"},
                    ],
                }
            ],
        }

        selection = workflow.normalize_selection(candidates, operator="codex")

        selected = selection["products"][0]["selectedCandidates"]
        self.assertEqual(["a", "b"], [item["candidateId"] for item in selected])
        self.assertTrue(selected[0]["isMain"])
        self.assertFalse(selected[1]["isMain"])

    def test_write_selection_round_trips_json(self) -> None:
        candidates = {
            "category": {"slug": "cable", "name": "Кабель"},
            "products": [{"productId": "p1", "externalId": "101", "name": "Кабель UTP", "candidates": []}],
        }

        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "selection.json"
            workflow.write_selection(path, workflow.normalize_selection(candidates, operator="codex"))

            data = json.loads(path.read_text(encoding="utf-8"))

        self.assertEqual("cable", data["category"]["slug"])
        self.assertEqual("codex", data["selectedByOperator"])
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: fail with `ImportError` or `AttributeError` because `product_image_review_workflow` does not exist yet.

- [ ] **Step 3: Implement minimal model helpers**

Create `tools/product_image_review_workflow.py` with:

```python
import argparse
import json
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MAX_SELECTED_PER_PRODUCT = 2


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def normalize_selection(candidates: dict, operator: str) -> dict:
    products = []
    selected_at = utc_now_iso()
    for product in candidates.get("products", []):
        selected = []
        for candidate in product.get("candidates", []):
            if not candidate.get("selected"):
                continue
            if len(selected) >= MAX_SELECTED_PER_PRODUCT:
                break
            selected.append(
                {
                    "candidateId": candidate["candidateId"],
                    "sourceSite": candidate.get("sourceSite", ""),
                    "sourcePageUrl": candidate.get("sourcePageUrl", ""),
                    "sourceImageUrl": candidate.get("sourceImageUrl", ""),
                    "isMain": len(selected) == 0,
                }
            )
        products.append(
            {
                "productId": product.get("productId"),
                "externalId": product.get("externalId"),
                "name": product.get("name"),
                "selectedCandidates": selected,
            }
        )

    return {
        "category": candidates.get("category", {}),
        "selectedByOperator": operator,
        "selectedAt": selected_at,
        "products": products,
    }


def write_json(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def write_selection(path: Path, selection: dict) -> None:
    write_json(path, selection)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--help-only", action="store_true")
    args = parser.parse_args()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 4: Run tests and verify pass**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: `Ran 2 tests`, `OK`.

- [ ] **Step 5: Commit**

```powershell
git add tools/product_image_review_workflow.py tests/tools/test_product_image_review_workflow.py
git commit -m "feat: add product image review selection model"
```

## Task 2: Candidate Filtering And Source Limits

**Files:**
- Modify: `tools/product_image_review_workflow.py`
- Modify: `tests/tools/test_product_image_review_workflow.py`

- [ ] **Step 1: Write failing tests for candidate filtering**

Append tests:

```python
    def test_filter_candidates_limits_to_two_per_source_and_rejects_documents(self) -> None:
        raw = [
            {"candidateId": "t1", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://www.tktdf.ru/a.png", "score": 90},
            {"candidateId": "t2", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://www.tktdf.ru/b.png", "score": 80},
            {"candidateId": "t3", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://www.tktdf.ru/c.png", "score": 70},
            {"candidateId": "g1", "sourceSite": "google", "sourceImageUrl": "https://site.test/cert.pdf", "score": 95},
            {"candidateId": "r1", "sourceSite": "redmrt.ru", "sourceImageUrl": "https://redmrt.ru/product.webp", "score": 60},
        ]

        filtered = workflow.filter_candidates(raw)

        self.assertEqual(["t1", "t2", "r1"], [item["candidateId"] for item in filtered])

    def test_build_product_candidates_marks_first_two_as_selected(self) -> None:
        product = {"productId": "p1", "externalId": "101", "name": "Кабель UTP"}
        candidates = [
            {"candidateId": "a", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://x/a.png", "score": 90},
            {"candidateId": "b", "sourceSite": "redmrt.ru", "sourceImageUrl": "https://x/b.png", "score": 80},
            {"candidateId": "c", "sourceSite": "google", "sourceImageUrl": "https://x/c.png", "score": 70},
        ]

        result = workflow.build_product_candidates(product, candidates)

        self.assertEqual([True, True, False], [item["selected"] for item in result["candidates"]])
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: fail because `filter_candidates` and `build_product_candidates` are missing.

- [ ] **Step 3: Implement filtering helpers**

Add to `tools/product_image_review_workflow.py`:

```python
REJECT_URL_PARTS = (
    "logo",
    "sprite",
    "icon",
    "favicon",
    "banner",
    "placeholder",
    "certificate",
    "certificates",
    "sert",
    "diplom",
    ".pdf",
    "/pdf/",
    "captcha",
)


def is_rejected_candidate_url(url: str) -> bool:
    lower = (url or "").lower()
    return any(part in lower for part in REJECT_URL_PARTS)


def filter_candidates(candidates: list[dict], max_per_source: int = 2, max_total: int = 6) -> list[dict]:
    by_source: dict[str, int] = {}
    accepted = []
    for candidate in sorted(candidates, key=lambda item: item.get("score", 0), reverse=True):
        if is_rejected_candidate_url(candidate.get("sourceImageUrl", "")):
            continue
        source = candidate.get("sourceSite", "")
        if by_source.get(source, 0) >= max_per_source:
            continue
        accepted.append(candidate)
        by_source[source] = by_source.get(source, 0) + 1
        if len(accepted) >= max_total:
            break
    return accepted


def build_product_candidates(product: dict, candidates: list[dict]) -> dict:
    filtered = filter_candidates(candidates)
    normalized = []
    for index, candidate in enumerate(filtered):
        item = dict(candidate)
        item["selected"] = index < MAX_SELECTED_PER_PRODUCT
        normalized.append(item)
    return {
        "productId": product.get("productId"),
        "externalId": product.get("externalId"),
        "sourceRows": product.get("sourceRows", []),
        "name": product.get("name"),
        "sku": product.get("sku"),
        "attributes": product.get("attributes", []),
        "candidates": normalized,
    }
```

- [ ] **Step 4: Run tests and verify pass**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add tools/product_image_review_workflow.py tests/tools/test_product_image_review_workflow.py
git commit -m "feat: filter product image candidates"
```

## Task 3: Candidate Source Collection

**Files:**
- Modify: `tools/product_image_review_workflow.py`
- Modify: `tests/tools/test_product_image_review_workflow.py`

- [ ] **Step 1: Write failing tests for source collection orchestration**

Append:

```python
    def test_collect_product_candidates_queries_all_sources_and_applies_limits(self) -> None:
        product = {"productId": "p1", "externalId": "101", "name": "Кабель UTP"}
        providers = [
            workflow.CandidateProvider(
                "tktdf.ru",
                lambda current: [
                    {"candidateId": "t1", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://www.tktdf.ru/1.png", "score": 90},
                    {"candidateId": "t2", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://www.tktdf.ru/2.png", "score": 80},
                    {"candidateId": "t3", "sourceSite": "tktdf.ru", "sourceImageUrl": "https://www.tktdf.ru/3.png", "score": 70},
                ],
            ),
            workflow.CandidateProvider(
                "redmrt.ru",
                lambda current: [
                    {"candidateId": "r1", "sourceSite": "redmrt.ru", "sourceImageUrl": "https://redmrt.ru/1.png", "score": 85}
                ],
            ),
            workflow.CandidateProvider(
                "google",
                lambda current: [
                    {"candidateId": "g1", "sourceSite": "google", "sourceImageUrl": "https://source.test/1.png", "score": 75}
                ],
            ),
        ]

        result = workflow.collect_product_candidates(product, providers)

        self.assertEqual(["t1", "r1", "t2", "g1"], [item["candidateId"] for item in result])
        self.assertLessEqual(sum(1 for item in result if item["sourceSite"] == "tktdf.ru"), 2)

    def test_google_provider_requires_visual_acceptance_for_default_selection(self) -> None:
        candidate = {
            "candidateId": "g1",
            "sourceSite": "google",
            "sourceImageUrl": "https://source.test/1.png",
            "score": 75,
            "visionStatus": "rejected",
        }

        product = workflow.build_product_candidates(
            {"productId": "p1", "externalId": "101", "name": "Кабель UTP"},
            [candidate],
        )

        self.assertFalse(product["candidates"][0]["selected"])
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: fail because `CandidateProvider` and `collect_product_candidates` are missing, and Google `visionStatus = rejected` is not yet considered by `build_product_candidates`.

- [ ] **Step 3: Implement provider orchestration**

Add:

```python
from dataclasses import dataclass
from typing import Callable


@dataclass(frozen=True)
class CandidateProvider:
    source_site: str
    collect: Callable[[dict], list[dict]]


def collect_product_candidates(product: dict, providers: list[CandidateProvider]) -> list[dict]:
    collected = []
    for provider in providers:
        for candidate in provider.collect(product):
            item = dict(candidate)
            item.setdefault("sourceSite", provider.source_site)
            collected.append(item)
    return filter_candidates(collected)
```

Update `build_product_candidates` so Google images rejected by machine vision are not selected by default:

```python
def build_product_candidates(product: dict, candidates: list[dict]) -> dict:
    filtered = filter_candidates(candidates)
    normalized = []
    selected_count = 0
    for candidate in filtered:
        item = dict(candidate)
        can_select = not (
            item.get("sourceSite") == "google"
            and item.get("visionStatus") not in {None, "accepted"}
        )
        item["selected"] = can_select and selected_count < MAX_SELECTED_PER_PRODUCT
        if item["selected"]:
            selected_count += 1
        normalized.append(item)
    return {
        "productId": product.get("productId"),
        "externalId": product.get("externalId"),
        "sourceRows": product.get("sourceRows", []),
        "name": product.get("name"),
        "sku": product.get("sku"),
        "attributes": product.get("attributes", []),
        "candidates": normalized,
    }
```

- [ ] **Step 4: Add concrete source input adapters**

Implement three deterministic adapter builders. They must not run network work during unit tests; each accepts already-loaded data or injectable fetch/search functions.

```python
def make_tktdf_provider(items: list[dict]) -> CandidateProvider:
    def collect(product: dict) -> list[dict]:
        query = " ".join([str(product.get("externalId") or ""), product.get("name") or ""]).lower()
        matches = []
        for item in items:
            haystack = " ".join([item.get("assetKey", ""), item.get("sourceProductUrl", ""), item.get("name", "")]).lower()
            if any(token and token in haystack for token in query.split()):
                matches.append(
                    {
                        "candidateId": f"tktdf-{len(matches) + 1}",
                        "sourceSite": "tktdf.ru",
                        "sourcePageUrl": item.get("sourceProductUrl"),
                        "sourceImageUrl": item.get("sourceImageUrl") or item.get("sourceProductUrl"),
                        "score": 90 - len(matches),
                    }
                )
        return matches
    return CandidateProvider("tktdf.ru", collect)


def make_redmrt_provider(items: list[dict]) -> CandidateProvider:
    def collect(product: dict) -> list[dict]:
        name_tokens = {token for token in (product.get("name") or "").lower().split() if len(token) > 2}
        matches = []
        for item in items:
            haystack = " ".join([item.get("title", ""), item.get("name", ""), item.get("url", "")]).lower()
            hits = sum(1 for token in name_tokens if token in haystack)
            if hits:
                matches.append(
                    {
                        "candidateId": f"redmrt-{len(matches) + 1}",
                        "sourceSite": "redmrt.ru",
                        "sourcePageUrl": item.get("url"),
                        "sourceImageUrl": item.get("image") or item.get("imageUrl"),
                        "score": 60 + hits,
                    }
                )
        return matches
    return CandidateProvider("redmrt.ru", collect)


def make_google_provider(search: Callable[[dict], list[dict]], vision_check: Callable[[dict, dict], dict]) -> CandidateProvider:
    def collect(product: dict) -> list[dict]:
        candidates = []
        for index, raw in enumerate(search(product)):
            checked = vision_check(product, raw)
            candidates.append(
                {
                    "candidateId": f"google-{index + 1}",
                    "sourceSite": "google",
                    "sourcePageUrl": raw.get("sourcePageUrl"),
                    "sourceImageUrl": raw.get("sourceImageUrl"),
                    "score": raw.get("score", 50),
                    "visionStatus": checked["status"],
                    "visionReason": checked["reason"],
                }
            )
        return candidates
    return CandidateProvider("google", collect)
```

- [ ] **Step 5: Add CLI command for building candidates from prepared source JSON**

Add subparser:

```python
    build = subparsers.add_parser("build-candidates")
    build.add_argument("--products", type=Path, required=True)
    build.add_argument("--category-slug", required=True)
    build.add_argument("--category-name", required=True)
    build.add_argument("--tktdf-source", type=Path)
    build.add_argument("--redmrt-source", type=Path)
    build.add_argument("--google-source", type=Path)
    build.add_argument("--output", type=Path, required=True)
```

Dispatch with prepared JSON sources:

```python
    if args.command == "build-candidates":
        products = json.loads(args.products.read_text(encoding="utf-8"))["products"]
        providers = []
        if args.tktdf_source:
            providers.append(make_tktdf_provider(json.loads(args.tktdf_source.read_text(encoding="utf-8")).get("items", [])))
        if args.redmrt_source:
            providers.append(make_redmrt_provider(json.loads(args.redmrt_source.read_text(encoding="utf-8"))))
        if args.google_source:
            google_items = json.loads(args.google_source.read_text(encoding="utf-8"))
            providers.append(
                make_google_provider(
                    lambda product: google_items.get(product.get("externalId") or "", []),
                    lambda product, raw: raw.get("vision", {"status": "rejected", "reason": "missing vision result"}),
                )
            )
        data = {
            "category": {"slug": args.category_slug, "name": args.category_name},
            "products": [build_product_candidates(product, collect_product_candidates(product, providers)) for product in products],
        }
        write_json(args.output, data)
        print(args.output)
        return 0
```

- [ ] **Step 6: Run tests**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add tools/product_image_review_workflow.py tests/tools/test_product_image_review_workflow.py
git commit -m "feat: collect product image candidates"
```

## Task 4: Browser Review HTML

**Files:**
- Modify: `tools/product_image_review_workflow.py`
- Modify: `tests/tools/test_product_image_review_workflow.py`

- [ ] **Step 1: Write failing HTML rendering test**

Append:

```python
    def test_render_review_html_contains_products_images_and_selection_script(self) -> None:
        candidates = {
            "category": {"slug": "cable", "name": "Кабель"},
            "products": [
                {
                    "productId": "p1",
                    "externalId": "101",
                    "name": "Кабель UTP",
                    "candidates": [
                        {
                            "candidateId": "a",
                            "sourceSite": "tktdf.ru",
                            "sourcePageUrl": "https://www.tktdf.ru/catalog/id/1/",
                            "sourceImageUrl": "https://www.tktdf.ru/a.png",
                            "selected": True,
                        }
                    ],
                }
            ],
        }

        html = workflow.render_review_html(candidates)

        self.assertIn("Кабель UTP", html)
        self.assertIn("https://www.tktdf.ru/a.png", html)
        self.assertIn("data-product-id=\"p1\"", html)
        self.assertIn("downloadSelection", html)
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: fail because `render_review_html` is missing.

- [ ] **Step 3: Implement static review page renderer**

Add imports and renderer:

```python
from html import escape


def render_review_html(candidates: dict) -> str:
    category = candidates.get("category", {})
    product_cards = []
    for product in candidates.get("products", []):
        candidate_cards = []
        for candidate in product.get("candidates", []):
            checked = " checked" if candidate.get("selected") else ""
            candidate_cards.append(
                f"""
                <label class="candidate">
                  <input type="checkbox"
                         data-product-id="{escape(str(product.get('productId') or ''))}"
                         data-candidate-id="{escape(str(candidate.get('candidateId') or ''))}"{checked}>
                  <img src="{escape(candidate.get('sourceImageUrl') or '')}" alt="">
                  <span class="candidate__meta">{escape(candidate.get('sourceSite') or '')}</span>
                  <a href="{escape(candidate.get('sourcePageUrl') or '')}" target="_blank" rel="noreferrer">Источник</a>
                </label>
                """
            )
        product_cards.append(
            f"""
            <article class="product" data-product-id="{escape(str(product.get('productId') or ''))}">
              <h2>{escape(product.get('name') or '')}</h2>
              <p>{escape(product.get('externalId') or '')}</p>
              <div class="candidates">{''.join(candidate_cards)}</div>
            </article>
            """
        )

    return f"""<!doctype html>
<html lang="ru">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Отбор изображений: {escape(category.get('name') or '')}</title>
  <style>
    body {{ font-family: Arial, sans-serif; margin: 24px; background: #f7f7f4; color: #1f2933; }}
    header {{ position: sticky; top: 0; background: #f7f7f4; padding: 12px 0; border-bottom: 1px solid #d6d3ca; }}
    .product {{ margin: 18px 0; padding: 16px; background: #fff; border: 1px solid #dedbd2; border-radius: 8px; }}
    .candidates {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 12px; }}
    .candidate {{ display: grid; gap: 8px; border: 1px solid #d6d3ca; border-radius: 8px; padding: 10px; background: #fbfaf7; }}
    .candidate img {{ width: 100%; aspect-ratio: 4 / 3; object-fit: contain; background: white; border: 1px solid #ece8df; }}
    .candidate__meta {{ font-size: 13px; color: #4b5563; }}
    button {{ padding: 10px 14px; border-radius: 6px; border: 1px solid #374151; background: #263238; color: white; }}
  </style>
</head>
<body>
  <header>
    <h1>{escape(category.get('name') or '')}</h1>
    <button type="button" onclick="downloadSelection()">Скачать selection.json</button>
  </header>
  <main>{''.join(product_cards)}</main>
  <script>
    const sourceData = {json.dumps(candidates, ensure_ascii=False)};
    function downloadSelection() {{
      const selectedByProduct = new Map();
      document.querySelectorAll('input[type="checkbox"]').forEach((input) => {{
        if (!input.checked) return;
        const list = selectedByProduct.get(input.dataset.productId) || [];
        if (list.length < 2) list.push(input.dataset.candidateId);
        selectedByProduct.set(input.dataset.productId, list);
      }});
      const products = sourceData.products.map((product) => ({{
        productId: product.productId,
        externalId: product.externalId,
        name: product.name,
        selectedCandidates: (selectedByProduct.get(product.productId) || []).map((candidateId, index) => {{
          const candidate = product.candidates.find((item) => item.candidateId === candidateId);
          return {{ ...candidate, isMain: index === 0 }};
        }})
      }}));
      const blob = new Blob([JSON.stringify({{
        category: sourceData.category,
        selectedByOperator: "browser",
        selectedAt: new Date().toISOString(),
        products
      }}, null, 2)], {{ type: "application/json" }});
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = "selection.json";
      link.click();
      URL.revokeObjectURL(url);
    }}
  </script>
</body>
</html>"""
```

- [ ] **Step 4: Add CLI command for review page generation**

Extend `main()` so the first production command works:

```python
    subparsers = parser.add_subparsers(dest="command", required=True)
    review = subparsers.add_parser("render-review")
    review.add_argument("--candidates", type=Path, required=True)
    review.add_argument("--output", type=Path, required=True)
```

Then dispatch:

```python
    if args.command == "render-review":
        data = json.loads(args.candidates.read_text(encoding="utf-8"))
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(render_review_html(data), encoding="utf-8")
        print(args.output)
        return 0
```

- [ ] **Step 5: Run tests**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```powershell
git add tools/product_image_review_workflow.py tests/tools/test_product_image_review_workflow.py
git commit -m "feat: render product image review page"
```

## Task 5: Finalize Selection Into PNG Manifest

**Files:**
- Modify: `tools/product_image_review_workflow.py`
- Modify: `tests/tools/test_product_image_review_workflow.py`

- [ ] **Step 1: Write failing test for selected image finalization**

Append:

```python
from io import BytesIO
from unittest.mock import patch
from PIL import Image


def make_png_bytes() -> bytes:
    buffer = BytesIO()
    Image.new("RGB", (32, 24), "white").save(buffer, "PNG")
    return buffer.getvalue()
```

Append test method:

```python
    def test_finalize_selection_downloads_selected_png_manifest(self) -> None:
        selection = {
            "category": {"slug": "cable", "name": "Кабель"},
            "selectedByOperator": "codex",
            "selectedAt": "2026-05-16T10:00:00Z",
            "products": [
                {
                    "productId": "p1",
                    "externalId": "101",
                    "name": "Кабель UTP",
                    "selectedCandidates": [
                        {
                            "candidateId": "a",
                            "sourceSite": "tktdf.ru",
                            "sourcePageUrl": "https://www.tktdf.ru/catalog/id/1/",
                            "sourceImageUrl": "https://www.tktdf.ru/a.png",
                            "isMain": True,
                        }
                    ],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            output_dir = root / "images"
            manifest_path = root / "manifest.json"
            with patch.object(workflow, "fetch", return_value=(make_png_bytes(), "image/png")):
                manifest = workflow.finalize_selection(selection, output_dir, manifest_path)

            item = manifest["items"][0]
            self.assertEqual("downloaded_png", item["status"])
            self.assertEqual("requires-permission", item["rightsStatus"])
            self.assertTrue(item["isMain"])
            self.assertTrue((root / item["file"]).exists())
            self.assertEqual(64, len(item["checksum"]))
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
```

Expected: fail because `fetch` and `finalize_selection` are missing.

- [ ] **Step 3: Implement download/finalize helpers**

Add:

```python
import hashlib
import re
from io import BytesIO
from urllib.request import Request, urlopen
from PIL import Image


USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome Safari"


def fetch(url: str, timeout: int = 25) -> tuple[bytes, str]:
    request = Request(url, headers={"User-Agent": USER_AGENT, "Accept": "*/*"})
    with urlopen(request, timeout=timeout) as response:
        return response.read(), response.headers.get("content-type", "")


def slug(value: str) -> str:
    value = (value or "").lower().strip()
    value = re.sub(r"[^a-z0-9._-]+", "-", value)
    return re.sub(r"-+", "-", value).strip("-") or "image"


def decode_image(body: bytes) -> Image.Image:
    image = Image.open(BytesIO(body))
    image.load()
    if image.width <= 0 or image.height <= 0:
        raise ValueError("image has empty dimensions")
    if image.mode not in {"RGB", "RGBA"}:
        image = image.convert("RGBA" if "A" in image.mode else "RGB")
    return image


def relative_to_root_or_parent(path: Path, root: Path) -> str:
    try:
        return str(path.relative_to(root)).replace("\\", "/")
    except ValueError:
        return str(path).replace("\\", "/")


def finalize_selection(selection: dict, output_dir: Path, manifest_path: Path) -> dict:
    output_dir.mkdir(parents=True, exist_ok=True)
    root = manifest_path.parent.parent.parent if len(manifest_path.parents) >= 3 else output_dir.parent
    items = []
    for product in selection.get("products", []):
        for index, candidate in enumerate(product.get("selectedCandidates", [])[:MAX_SELECTED_PER_PRODUCT]):
            body, content_type = fetch(candidate["sourceImageUrl"])
            checksum = hashlib.sha256(body).hexdigest()
            image = decode_image(body)
            asset_key = slug(f"{product.get('externalId')}-{candidate.get('candidateId')}")
            target = output_dir / f"{asset_key}.png"
            image.convert("RGBA").save(target, "PNG", optimize=True)
            items.append(
                {
                    "assetKey": asset_key,
                    "productId": product.get("productId"),
                    "externalId": product.get("externalId"),
                    "sourceRows": product.get("sourceRows", []),
                    "sourceSite": candidate.get("sourceSite"),
                    "sourcePageUrl": candidate.get("sourcePageUrl"),
                    "sourceImageUrl": candidate.get("sourceImageUrl"),
                    "status": "downloaded_png",
                    "file": relative_to_root_or_parent(target, root),
                    "width": image.width,
                    "height": image.height,
                    "checksum": checksum,
                    "contentType": "image/png",
                    "originalContentType": content_type,
                    "isMain": bool(candidate.get("isMain", index == 0)),
                    "visualReviewStatus": "accepted_operator_review",
                    "rightsStatus": candidate.get("rightsStatus", "requires-permission"),
                    "selectedByOperator": selection.get("selectedByOperator"),
                    "selectedAt": selection.get("selectedAt"),
                }
            )
    manifest = {
        "category": selection.get("category", {}),
        "outputDir": relative_to_root_or_parent(output_dir, root),
        "downloadedPng": len(items),
        "items": items,
    }
    write_json(manifest_path, manifest)
    return manifest
```

- [ ] **Step 4: Add CLI command for finalization**

Add subparser:

```python
    finalize = subparsers.add_parser("finalize-selection")
    finalize.add_argument("--selection", type=Path, required=True)
    finalize.add_argument("--output-dir", type=Path, required=True)
    finalize.add_argument("--manifest", type=Path, required=True)
```

Dispatch:

```python
    if args.command == "finalize-selection":
        selection = json.loads(args.selection.read_text(encoding="utf-8"))
        manifest = finalize_selection(selection, args.output_dir, args.manifest)
        print(f"downloaded_png: {manifest['downloadedPng']}")
        return 0
```

- [ ] **Step 5: Run tests and JSON validation**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
python tools\product_image_review_workflow.py --help
```

Expected: tests pass; help lists `render-review` and `finalize-selection`.

- [ ] **Step 6: Commit**

```powershell
git add tools/product_image_review_workflow.py tests/tools/test_product_image_review_workflow.py
git commit -m "feat: finalize reviewed product images"
```

## Task 6: Reviewed Manifest Reader In Catalog Import Core

**Files:**
- Create: `apps/catalog-import.core/Images/ReviewedProductImageManifestReader.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageManifestReaderTests.cs`

- [ ] **Step 1: Write failing C# manifest reader tests**

Create test file:

```csharp
using System.Text.Json;
using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ReviewedProductImageManifestReaderTests
{
    [Fact]
    public void ReadAcceptedGroupsItemsByExternalIdAndKeepsMainOrder()
    {
        using var temp = new TemporaryDirectory();
        var image = Path.Combine(temp.Path, "101-a.png");
        File.WriteAllText(image, "png");
        var manifest = Path.Combine(temp.Path, "manifest.json");
        File.WriteAllText(
            manifest,
            JsonSerializer.Serialize(new
            {
                items = new object[]
                {
                    new
                    {
                        assetKey = "101-a",
                        externalId = "101",
                        status = "downloaded_png",
                        file = image,
                        checksum = new string('a', 64),
                        contentType = "image/png",
                        isMain = true,
                        visualReviewStatus = "accepted_operator_review",
                        rightsStatus = "requires-permission"
                    },
                    new
                    {
                        assetKey = "101-b",
                        externalId = "101",
                        status = "downloaded_png",
                        file = image,
                        checksum = new string('b', 64),
                        contentType = "image/png",
                        isMain = false,
                        visualReviewStatus = "accepted_operator_review",
                        rightsStatus = "requires-permission"
                    }
                }
            }),
            System.Text.Encoding.UTF8);

        var result = ReviewedProductImageManifestReader.ReadAcceptedByExternalId(manifest);

        Assert.True(result.ContainsKey("101"));
        Assert.Equal(2, result["101"].Count);
        Assert.True(result["101"][0].IsMain);
        Assert.False(result["101"][1].IsMain);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImageManifestReaderTests
```

Expected: compile failure because reader does not exist.

- [ ] **Step 3: Implement reader**

Create:

```csharp
using System.Text.Json;

namespace LineCom.CatalogImport.Core.Images;

public sealed record ReviewedProductImageManifestItem(
    string AssetKey,
    string ExternalId,
    string File,
    string Checksum,
    string ContentType,
    bool IsMain,
    string RightsStatus);

public static class ReviewedProductImageManifestReader
{
    private const string AcceptedStatus = "downloaded_png";
    private const string AcceptedReviewStatus = "accepted_operator_review";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<ReviewedProductImageManifestItem>> ReadAcceptedByExternalId(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new Dictionary<string, IReadOnlyList<ReviewedProductImageManifestItem>>(StringComparer.Ordinal);
        }

        using var stream = File.OpenRead(path);
        var manifest = JsonSerializer.Deserialize<ReviewedProductImageManifest>(stream, JsonOptions);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        var groups = new Dictionary<string, List<ReviewedProductImageManifestItem>>(StringComparer.Ordinal);
        foreach (var entry in manifest?.Items ?? [])
        {
            if (!IsAccepted(entry))
            {
                continue;
            }

            var item = new ReviewedProductImageManifestItem(
                entry.AssetKey!,
                entry.ExternalId!,
                ResolveImagePath(entry.File!, directory),
                entry.Checksum!,
                entry.ContentType!,
                entry.IsMain,
                string.IsNullOrWhiteSpace(entry.RightsStatus) ? "requires-permission" : entry.RightsStatus!);
            if (!groups.TryGetValue(item.ExternalId, out var list))
            {
                list = [];
                groups[item.ExternalId] = list;
            }

            list.Add(item);
        }

        return groups.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ReviewedProductImageManifestItem>)pair.Value
                .OrderByDescending(item => item.IsMain)
                .ThenBy(item => item.AssetKey, StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
    }

    private static bool IsAccepted(ReviewedProductImageManifestEntry item)
    {
        return string.Equals(item.Status, AcceptedStatus, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.VisualReviewStatus, AcceptedReviewStatus, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(item.AssetKey)
            && !string.IsNullOrWhiteSpace(item.ExternalId)
            && !string.IsNullOrWhiteSpace(item.File)
            && !string.IsNullOrWhiteSpace(item.Checksum)
            && string.Equals(item.ContentType, "image/png", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveImagePath(string filePath, string? manifestDirectory)
    {
        if (Path.IsPathFullyQualified(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        var directory = string.IsNullOrWhiteSpace(manifestDirectory)
            ? null
            : new DirectoryInfo(manifestDirectory);
        while (directory is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(directory.FullName, filePath));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return filePath;
    }

    private sealed class ReviewedProductImageManifest
    {
        public IReadOnlyList<ReviewedProductImageManifestEntry> Items { get; init; } = [];
    }

    private sealed class ReviewedProductImageManifestEntry
    {
        public string? AssetKey { get; init; }
        public string? ExternalId { get; init; }
        public string? Status { get; init; }
        public string? File { get; init; }
        public string? Checksum { get; init; }
        public string? ContentType { get; init; }
        public bool IsMain { get; init; }
        public string? VisualReviewStatus { get; init; }
        public string? RightsStatus { get; init; }
    }
}
```

- [ ] **Step 4: Run test**

Run:

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImageManifestReaderTests
```

Expected: pass.

- [ ] **Step 5: Commit**

```powershell
git add apps/catalog-import.core/Images/ReviewedProductImageManifestReader.cs tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageManifestReaderTests.cs
git commit -m "feat: read reviewed product image manifests"
```

## Task 7: Dry-Run And Apply Service SQL Boundary

**Files:**
- Create: `apps/catalog-import.core/Images/ReviewedProductImageApplyService.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageApplySqlTests.cs`

- [ ] **Step 1: Write SQL boundary tests**

Create:

```csharp
using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ReviewedProductImageApplySqlTests
{
    [Fact]
    public void SelectExistingImagesUsesExternalIdAndCountsCurrentProductImages()
    {
        Assert.Contains("WHERE product.external_id = @ExternalId", ReviewedProductImageApplySql.SelectProductImageState);
        Assert.Contains("COUNT(image.id)", ReviewedProductImageApplySql.SelectProductImageState);
    }

    [Fact]
    public void InsertProductImageDoesNotClearExistingMainImage()
    {
        Assert.DoesNotContain("DELETE FROM product_images", ReviewedProductImageApplySql.InsertProductImage);
        Assert.DoesNotContain("UPDATE product_images", ReviewedProductImageApplySql.InsertProductImage);
        Assert.Contains("ON CONFLICT (product_id, stored_file_id) DO NOTHING", ReviewedProductImageApplySql.InsertProductImage);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

Run:

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImageApplySqlTests
```

Expected: compile failure because SQL class does not exist.

- [ ] **Step 3: Implement SQL constants and result records**

Create `ReviewedProductImageApplyService.cs` with the first slice:

```csharp
using Dapper;
using Npgsql;

namespace LineCom.CatalogImport.Core.Images;

public static class ReviewedProductImageApplySql
{
    public const string SelectProductImageState = """
        SELECT
            product.id AS "ProductId",
            product.name AS "ProductName",
            COUNT(image.id) AS "ImagesCount",
            BOOL_OR(image.is_main) AS "HasMainImage"
        FROM products product
        LEFT JOIN product_images image ON image.product_id = product.id
        WHERE product.external_id = @ExternalId
        GROUP BY product.id, product.name;
        """;

    public const string InsertStoredFile = """
        INSERT INTO stored_files (
            storage_key,
            original_file_name,
            content_type,
            size_bytes,
            checksum,
            purpose,
            status)
        VALUES (
            @StorageKey,
            @OriginalFileName,
            'image/png',
            @SizeBytes,
            @Checksum,
            'product_image',
            'active')
        ON CONFLICT (storage_key) DO NOTHING
        RETURNING id;
        """;

    public const string SelectStoredFile = """
        SELECT id
        FROM stored_files
        WHERE storage_key = @StorageKey
          AND checksum = @Checksum
          AND content_type = 'image/png'
          AND purpose = 'product_image'
          AND status = 'active';
        """;

    public const string InsertProductImage = """
        INSERT INTO product_images (
            product_id,
            stored_file_id,
            alt,
            title,
            sort_order,
            is_main)
        VALUES (
            @ProductId,
            @StoredFileId,
            @Alt,
            @Title,
            @SortOrder,
            @IsMain)
        ON CONFLICT (product_id, stored_file_id) DO NOTHING;
        """;
}

public sealed record ReviewedProductImageApplyOptions(
    string ConnectionString,
    string StorageRootPath,
    bool Apply,
    bool AllowAddToProductsWithExistingImages);

public sealed record ReviewedProductImageApplyResult(
    int Planned,
    int Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Errors);
```

- [ ] **Step 4: Run SQL tests**

Run:

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImageApplySqlTests
```

Expected: pass.

- [ ] **Step 5: Commit**

```powershell
git add apps/catalog-import.core/Images/ReviewedProductImageApplyService.cs tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageApplySqlTests.cs
git commit -m "feat: define reviewed image apply SQL"
```

## Task 8: Apply Service Implementation

**Files:**
- Modify: `apps/catalog-import.core/Images/ReviewedProductImageApplyService.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageApplyServiceTests.cs`

- [ ] **Step 1: Write unit tests for dry-run skip behavior**

Use fake manifest items and an in-memory product-state dictionary. Extract planning into a pure method:

```csharp
using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ReviewedProductImageApplyServiceTests
{
    [Fact]
    public void PlanSkipsProductWithExistingImagesUnlessAllowed()
    {
        var items = new[]
        {
            new ReviewedProductImageManifestItem("101-a", "101", "image.png", new string('a', 64), "image/png", true, "requires-permission")
        };
        var states = new Dictionary<string, ReviewedProductImageProductState>(StringComparer.Ordinal)
        {
            ["101"] = new ReviewedProductImageProductState(Guid.NewGuid(), "Кабель UTP", 1, true)
        };

        var plan = ReviewedProductImageApplyPlanner.Plan(items, states, allowAddToProductsWithExistingImages: false);

        Assert.Empty(plan.Apply);
        Assert.Contains(plan.Skip, item => item.ExternalId == "101" && item.Reason.Contains("already has images", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 2: Run test and verify failure**

Run:

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImageApplyServiceTests
```

Expected: compile failure for missing planner/state types.

- [ ] **Step 3: Implement pure planner and service shell**

Add:

```csharp
public sealed record ReviewedProductImageProductState(
    Guid ProductId,
    string ProductName,
    int ImagesCount,
    bool HasMainImage);

public sealed record ReviewedProductImageApplyPlan(
    IReadOnlyList<ReviewedProductImageApplyPlanItem> Apply,
    IReadOnlyList<ReviewedProductImageSkip> Skip);

public sealed record ReviewedProductImageApplyPlanItem(
    ReviewedProductImageManifestItem Image,
    ReviewedProductImageProductState Product,
    int SortOrder,
    bool IsMain);

public sealed record ReviewedProductImageSkip(string ExternalId, string AssetKey, string Reason);

public static class ReviewedProductImageApplyPlanner
{
    public static ReviewedProductImageApplyPlan Plan(
        IReadOnlyList<ReviewedProductImageManifestItem> images,
        IReadOnlyDictionary<string, ReviewedProductImageProductState> states,
        bool allowAddToProductsWithExistingImages)
    {
        var apply = new List<ReviewedProductImageApplyPlanItem>();
        var skip = new List<ReviewedProductImageSkip>();
        foreach (var group in images.GroupBy(item => item.ExternalId, StringComparer.Ordinal))
        {
            if (!states.TryGetValue(group.Key, out var state))
            {
                foreach (var image in group)
                {
                    skip.Add(new ReviewedProductImageSkip(group.Key, image.AssetKey, "Product was not found."));
                }
                continue;
            }

            if (state.ImagesCount > 0 && !allowAddToProductsWithExistingImages)
            {
                foreach (var image in group)
                {
                    skip.Add(new ReviewedProductImageSkip(group.Key, image.AssetKey, "Product already has images."));
                }
                continue;
            }

            var ordered = group.Take(2).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                var image = ordered[index];
                var isMain = state.ImagesCount == 0 && index == 0 && !state.HasMainImage;
                apply.Add(new ReviewedProductImageApplyPlanItem(image, state, index, isMain));
            }
        }

        return new ReviewedProductImageApplyPlan(apply, skip);
    }
}
```

- [ ] **Step 4: Add DB-backed apply method**

Implement service that:

1. Reads manifest with `ReviewedProductImageManifestReader`.
2. Queries product states by `externalId`.
3. Builds a plan.
4. If `Apply == false`, returns planned/skipped counts without writing.
5. If `Apply == true`, copies PNG files to `storage/products/reviewed/<assetKey>-<checksum-prefix>.png`, inserts `stored_files`, inserts `product_images`.

Use storage key formatting:

```csharp
private static string FormatStorageKey(string assetKey, string checksum)
{
    var prefix = checksum[..Math.Min(12, checksum.Length)];
    return $"storage/products/reviewed/{assetKey}-{prefix}.png";
}
```

Use physical copy guard:

```csharp
private static string ResolveStoragePath(string storageRootPath, string storageKey)
{
    var relative = storageKey["storage/".Length..].Replace('/', Path.DirectorySeparatorChar);
    var root = Path.GetFullPath(storageRootPath);
    var path = Path.GetFullPath(Path.Combine(root, relative));
    var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
    if (!path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Storage path escapes the configured root.");
    }
    return path;
}
```

- [ ] **Step 5: Run targeted tests**

Run:

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "ReviewedProductImageApply"
```

Expected: all reviewed image apply tests pass.

- [ ] **Step 6: Commit**

```powershell
git add apps/catalog-import.core/Images/ReviewedProductImageApplyService.cs tests/LineCom.Api.Tests/CatalogImport/ReviewedProductImageApplyServiceTests.cs
git commit -m "feat: apply reviewed product images"
```

## Task 9: End-To-End Review Fixture And Playwright QA

**Files:**
- Create: `Assets/product-image-review/sample/candidates.json`
- Modify: `vault/Человекочитаемое/Catalog Image Import iterations.md`

- [ ] **Step 1: Create a small sample candidates file**

Create `Assets/product-image-review/sample/candidates.json`:

```json
{
  "category": { "slug": "sample", "name": "Тестовая категория" },
  "products": [
    {
      "productId": "sample-product",
      "externalId": "sample-101",
      "sourceRows": [101],
      "name": "Тестовый кабель UTP",
      "sku": "UTP-SAMPLE",
      "attributes": ["cat.5e", "305 м"],
      "candidates": [
        {
          "candidateId": "sample-a",
          "sourceSite": "tktdf.ru",
          "sourcePageUrl": "https://www.tktdf.ru/catalog/id/51108/",
          "sourceImageUrl": "https://www.tktdf.ru/upload/iblock/sample/product.png",
          "score": 90,
          "selected": true
        }
      ]
    }
  ]
}
```

- [ ] **Step 2: Render review HTML**

Run:

```powershell
python tools\product_image_review_workflow.py render-review --candidates Assets\product-image-review\sample\candidates.json --output Assets\product-image-review\sample\review.html
```

Expected: prints `Assets\product-image-review\sample\review.html`.

- [ ] **Step 3: Open with Browser/Playwright and verify visibility**

Use the in-app browser or Playwright to open the local file. Verify:

- category title is visible;
- product name is visible;
- candidate checkbox is checked;
- source link is visible;
- screenshot is nonblank.

The Playwright Python API supports `page.goto(...)`, semantic locators like `page.get_by_role(...)`, checkbox interactions through locator `.check()`, and screenshots with `page.screenshot(...)`; this was verified against current Context7 docs for `/microsoft/playwright-python`.

- [ ] **Step 4: Update documentation**

Append an iteration note to `vault/Человекочитаемое/Catalog Image Import iterations.md`:

```markdown
## 2026-05-16. Product image review workflow

Цель: подготовить управляемый процесс отбора изображений по категориям перед загрузкой в Local FileStorage.

Решения:

- поиск кандидатов разделен от применения к БД;
- операторский выбор хранится в `selection.json`;
- финальные PNG и manifest хранятся в `Assets/product-images/reviewed/<category-slug>/`;
- внешние изображения сохраняют `rightsStatus = requires-permission`;
- товары с существующими изображениями по умолчанию не перезаписываются.

Проверки:

- `python -m unittest tests.tools.test_product_image_review_workflow`;
- `dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImage`;
- ручная проверка `Assets/product-image-review/sample/review.html` в браузере.
```

- [ ] **Step 5: Run verification commands**

Run:

```powershell
python -m unittest tests.tools.test_product_image_review_workflow
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter ReviewedProductImage
python -m json.tool Assets\product-image-review\sample\candidates.json
```

Expected: all pass.

- [ ] **Step 6: Commit**

```powershell
git add Assets/product-image-review/sample/candidates.json Assets/product-image-review/sample/review.html "vault/Человекочитаемое/Catalog Image Import iterations.md"
git commit -m "docs: document product image review workflow"
```

## Task 10: Final Full Verification

**Files:**
- No code changes unless verification finds a defect.

- [ ] **Step 1: Run Python tests**

```powershell
python -m unittest tests.tools.test_product_image_review_workflow tests.tools.test_download_tktdf_product_images
```

Expected: all Python tests pass.

- [ ] **Step 2: Run targeted .NET tests**

```powershell
dotnet test tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "CatalogImport|ReviewedProductImage"
```

Expected: tests pass. If NuGet vulnerability feed is unavailable, record `NU1900` as external feed warning only when tests still pass.

- [ ] **Step 3: Run full build if targeted checks pass**

```powershell
dotnet build LineCom.sln -m:1
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 4: Check for accidental debt markers in touched files**

```powershell
rg -n "TO[D]O|TB[D]|FIX[M]E|заглу[ш]|косты[л]|temporar[y]|hac[k]" tools tests apps\catalog-import.core vault\Человекочитаемое docs\superpowers
```

Expected: no new debt markers in touched implementation files.

- [ ] **Step 5: Final status**

Summarize:

- created commands;
- where review artifacts are stored;
- how to render a category review page;
- how to finalize `selection.json`;
- how dry-run/apply protects existing product images;
- test commands and results.
