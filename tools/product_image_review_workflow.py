import argparse
import json
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable


ROOT = Path(__file__).resolve().parents[1]
MAX_SELECTED_PER_PRODUCT = 2
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


@dataclass(frozen=True)
class CandidateProvider:
    source_site: str
    collect: Callable[[dict], list[dict]]


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


def collect_product_candidates(product: dict, providers: list[CandidateProvider]) -> list[dict]:
    collected = []
    for provider in providers:
        for candidate in provider.collect(product):
            item = dict(candidate)
            item.setdefault("sourceSite", provider.source_site)
            collected.append(item)
    return filter_candidates(collected)


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


def write_json(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def write_selection(path: Path, selection: dict) -> None:
    write_json(path, selection)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--help-only", action="store_true")
    subparsers = parser.add_subparsers(dest="command")
    build = subparsers.add_parser("build-candidates")
    build.add_argument("--products", type=Path, required=True)
    build.add_argument("--category-slug", required=True)
    build.add_argument("--category-name", required=True)
    build.add_argument("--tktdf-source", type=Path)
    build.add_argument("--redmrt-source", type=Path)
    build.add_argument("--google-source", type=Path)
    build.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
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
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
