import argparse
import json
import mimetypes
import re
import time
from html import unescape
from pathlib import Path
from urllib.parse import urljoin, urlparse
from urllib.request import Request, urlopen


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "Assets" / "product_image_candidates_part1.json"
DEFAULT_OUTPUT_DIR = ROOT / "Assets" / "product-images" / "part1"
DEFAULT_MANIFEST = ROOT / "Assets" / "product-images" / "part1_manifest.json"

USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) "
    "Chrome/124.0 Safari/537.36"
)

SKIP_URL_PARTS = (
    "logo",
    "sprite",
    "icon",
    "favicon",
    "banner",
    "captcha",
    "youtube",
    "telegram",
    "rutube",
)


def slug_filename(value: str) -> str:
    value = value.lower().strip()
    value = re.sub(r"[^a-z0-9._-]+", "-", value)
    value = re.sub(r"-+", "-", value).strip("-")
    return value or "image"


def fetch(url: str, timeout: int = 25) -> tuple[bytes, str]:
    req = Request(url, headers={"User-Agent": USER_AGENT, "Accept": "*/*"})
    with urlopen(req, timeout=timeout) as response:
        return response.read(), response.headers.get("content-type", "")


def extension_from_content_type(content_type: str, fallback_url: str) -> str:
    content_type = (content_type or "").split(";")[0].strip().lower()
    ext = mimetypes.guess_extension(content_type) if content_type else None
    if ext == ".jpe":
        ext = ".jpg"
    if ext:
        return ext

    suffix = Path(urlparse(fallback_url).path).suffix.lower()
    if suffix in {".jpg", ".jpeg", ".png", ".webp", ".gif"}:
        return ".jpg" if suffix == ".jpeg" else suffix
    return ".jpg"


def extract_meta_images(html: str, page_url: str) -> list[dict]:
    candidates = []
    meta_pattern = re.compile(
        r"<meta\b[^>]*(?:property|name)=[\"'](?:og:image|twitter:image)[\"'][^>]*>",
        re.I,
    )
    content_pattern = re.compile(r"\bcontent=[\"']([^\"']+)[\"']", re.I)
    for tag in meta_pattern.findall(html):
        match = content_pattern.search(tag)
        if match:
            candidates.append(
                {
                    "url": urljoin(page_url, unescape(match.group(1))),
                    "score": 120,
                    "reason": "meta image",
                }
            )
    return candidates


def extract_img_candidates(html: str, page_url: str, asset_key: str, matched_title: str) -> list[dict]:
    candidates = []
    wanted = " ".join([asset_key, matched_title]).lower()
    img_pattern = re.compile(r"<img\b[^>]*>", re.I)
    attr_pattern = re.compile(r"([a-zA-Z_:.-]+)\s*=\s*([\"'])(.*?)\2", re.S)

    for tag in img_pattern.findall(html):
        attrs = {name.lower(): unescape(value.strip()) for name, _, value in attr_pattern.findall(tag)}
        src = (
            attrs.get("src")
            or attrs.get("data-src")
            or attrs.get("data-original")
            or attrs.get("data-lazy-src")
            or attrs.get("data-url")
        )
        srcset = attrs.get("srcset") or attrs.get("data-srcset")
        if srcset:
            first = srcset.split(",")[0].strip().split(" ")[0]
            src = src or first
        if not src:
            continue

        image_url = urljoin(page_url, src)
        lower_url = image_url.lower()
        if not lower_url.startswith(("http://", "https://")):
            continue
        if any(part in lower_url for part in SKIP_URL_PARTS):
            continue

        text = " ".join([attrs.get("alt", ""), attrs.get("title", ""), image_url]).lower()
        score = 20
        if attrs.get("alt") or attrs.get("title"):
            score += 20
        for token in re.findall(r"[a-z0-9]{2,}", wanted):
            if token in text:
                score += 4
        if any(ext in lower_url for ext in (".jpg", ".jpeg", ".png", ".webp")):
            score += 10
        if any(part in lower_url for part in ("product", "catalog", "upload", "images", "photo")):
            score += 10

        candidates.append({"url": image_url, "score": score, "reason": "img tag"})

    candidates.sort(key=lambda item: item["score"], reverse=True)
    return candidates


def candidate_images_from_page(page_url: str, asset_key: str, matched_title: str) -> list[dict]:
    body, content_type = fetch(page_url)
    if "html" not in content_type.lower() and not body.lstrip().startswith(b"<"):
        return [{"url": page_url, "score": 100, "reason": "direct image"}]
    html = body.decode("utf-8", errors="replace")
    candidates = extract_meta_images(html, page_url)
    candidates.extend(extract_img_candidates(html, page_url, asset_key, matched_title))

    seen = set()
    unique = []
    for candidate in sorted(candidates, key=lambda item: item["score"], reverse=True):
        url = candidate["url"]
        if url in seen:
            continue
        seen.add(url)
        unique.append(candidate)
    return unique


def download_one(asset: dict, output_dir: Path) -> dict:
    asset_key = asset["assetKey"]
    errors = []

    for page in asset.get("candidatePages", []):
        page_url = page["url"]
        matched_title = page.get("matchedTitle", "")
        try:
            image_candidates = candidate_images_from_page(page_url, asset_key, matched_title)
        except Exception as exc:
            errors.append({"pageUrl": page_url, "error": f"page fetch failed: {exc}"})
            continue

        for candidate in image_candidates[:8]:
            image_url = candidate["url"]
            try:
                body, content_type = fetch(image_url)
                if len(body) < 2048:
                    errors.append({"imageUrl": image_url, "error": "too small"})
                    continue
                if not (
                    "image/" in content_type.lower()
                    or Path(urlparse(image_url).path).suffix.lower() in {".jpg", ".jpeg", ".png", ".webp"}
                ):
                    errors.append({"imageUrl": image_url, "error": f"not an image: {content_type}"})
                    continue

                ext = extension_from_content_type(content_type, image_url)
                filename = f"{slug_filename(asset_key)}{ext}"
                target = output_dir / filename
                target.write_bytes(body)
                return {
                    "assetKey": asset_key,
                    "status": "downloaded",
                    "file": str(target.relative_to(ROOT)).replace("\\", "/"),
                    "sourcePageUrl": page_url,
                    "imageUrl": image_url,
                    "contentType": content_type,
                    "bytes": len(body),
                    "matchConfidence": asset.get("matchConfidence"),
                    "rightsStatus": asset.get("rightsStatus"),
                }
            except Exception as exc:
                errors.append({"imageUrl": image_url, "error": str(exc)})

    return {
        "assetKey": asset_key,
        "status": "failed",
        "matchConfidence": asset.get("matchConfidence"),
        "rightsStatus": asset.get("rightsStatus"),
        "errors": errors[-10:],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--delay", type=float, default=0.3)
    args = parser.parse_args()

    data = json.loads(args.source.read_text(encoding="utf-8"))
    args.output_dir.mkdir(parents=True, exist_ok=True)
    args.manifest.parent.mkdir(parents=True, exist_ok=True)

    results = []
    for asset in data.get("imageAssets", []):
        result = download_one(asset, args.output_dir)
        results.append(result)
        print(f"{result['status']}: {asset['assetKey']}")
        time.sleep(args.delay)

    manifest = {
        "source": str(args.source.relative_to(ROOT)).replace("\\", "/"),
        "outputDir": str(args.output_dir.relative_to(ROOT)).replace("\\", "/"),
        "total": len(results),
        "downloaded": sum(1 for item in results if item["status"] == "downloaded"),
        "failed": sum(1 for item in results if item["status"] == "failed"),
        "rightsNote": data.get("batch", {}).get("rightsNote"),
        "items": results,
    }
    args.manifest.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0 if manifest["downloaded"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
