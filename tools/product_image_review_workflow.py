import argparse
import json
from datetime import datetime, timezone
from pathlib import Path


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
