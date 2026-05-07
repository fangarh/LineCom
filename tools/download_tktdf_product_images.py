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
