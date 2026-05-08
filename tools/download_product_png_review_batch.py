import argparse
import json
import re
import time
from html import unescape
from io import BytesIO
from pathlib import Path
from urllib.parse import urljoin, urlparse
from urllib.request import Request, urlopen

from PIL import Image, ImageDraw, ImageFont, ImageStat


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "Assets" / "product_image_candidates_part1.json"
DEFAULT_OUTPUT_DIR = ROOT / "Assets" / "product-images" / "part1_png_reviewed"
DEFAULT_MANIFEST = ROOT / "Assets" / "product-images" / "part1_png_reviewed_manifest.json"
DEFAULT_CONTACT_SHEET = ROOT / "Assets" / "product-images" / "part1_png_reviewed_contact_sheet.png"

USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) "
    "Chrome/124.0 Safari/537.36"
)

URL_REJECT_PARTS = (
    "logo",
    "sprite",
    "icon",
    "favicon",
    "captcha",
    "banner",
    "placeholder",
    "certificate",
    "certificates",
    "sert",
    "diplom",
    "doc",
    "loader",
    "preload",
    "youtube",
    "telegram",
    "whatsapp",
)

HTML_IMAGE_ATTRS = (
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
    req = Request(url, headers={"User-Agent": USER_AGENT, "Accept": "*/*"})
    with urlopen(req, timeout=timeout) as response:
        return response.read(), response.headers.get("content-type", "")


def slug(value: str) -> str:
    value = value.lower().strip()
    value = re.sub(r"[^a-z0-9._-]+", "-", value)
    return re.sub(r"-+", "-", value).strip("-") or "image"


def text_tokens(value: str) -> set[str]:
    return {token for token in re.findall(r"[a-zа-я0-9]{2,}", value.lower()) if len(token) > 1}


def extract_candidate_urls(html: str, page_url: str, asset: dict, page: dict) -> list[dict]:
    candidates = []
    wanted_tokens = text_tokens(" ".join([asset.get("assetKey", ""), page.get("matchedTitle", "")]))

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
                    "score": 80,
                    "reason": "meta image",
                }
            )

    img_pattern = re.compile(r"<img\b[^>]*>", re.I)
    attr_pattern = re.compile(r"([a-zA-Z_:.-]+)\s*=\s*([\"'])(.*?)\2", re.S)
    for tag in img_pattern.findall(html):
        attrs = {name.lower(): unescape(value.strip()) for name, _, value in attr_pattern.findall(tag)}
        urls = []
        for attr in HTML_IMAGE_ATTRS:
            if attrs.get(attr):
                urls.append(attrs[attr])
        srcset = attrs.get("srcset") or attrs.get("data-srcset")
        if srcset:
            urls.extend(part.strip().split(" ")[0] for part in srcset.split(",") if part.strip())

        surrounding_text = " ".join([attrs.get("alt", ""), attrs.get("title", ""), tag]).lower()
        for raw_url in urls:
            image_url = urljoin(page_url, raw_url)
            lower_url = image_url.lower()
            if not lower_url.startswith(("http://", "https://")):
                continue
            if any(part in lower_url for part in URL_REJECT_PARTS):
                continue

            token_hits = sum(1 for token in wanted_tokens if token in lower_url or token in surrounding_text)
            score = 25 + token_hits * 5
            if any(part in lower_url for part in ("product", "catalog", "upload", "images", "photo", "goods")):
                score += 15
            if attrs.get("alt") or attrs.get("title"):
                score += 10
            candidates.append({"url": image_url, "score": score, "reason": "img tag"})

    unique = {}
    for item in candidates:
        lower_url = item["url"].lower()
        if any(part in lower_url for part in URL_REJECT_PARTS):
            continue
        if item["url"] not in unique or item["score"] > unique[item["url"]]["score"]:
            unique[item["url"]] = item
    return sorted(unique.values(), key=lambda item: item["score"], reverse=True)


def image_quality_score(image: Image.Image) -> tuple[bool, int, list[str]]:
    reasons = []
    width, height = image.size
    if width < 180 or height < 120:
        reasons.append(f"small:{width}x{height}")
    ratio = width / max(height, 1)
    if ratio > 8 or ratio < 0.12:
        reasons.append(f"bad-ratio:{ratio:.2f}")

    rgb = image.convert("RGB")
    stat = ImageStat.Stat(rgb.resize((64, 64)))
    variance = sum(stat.var) / 3
    if variance < 80:
        reasons.append("low-visual-variance")

    score = width * height
    if image.mode in {"RGBA", "LA"}:
        score += 10_000
    return not reasons, score, reasons


def open_candidate_image(body: bytes) -> Image.Image:
    image = Image.open(BytesIO(body))
    image.load()
    if image.mode not in {"RGB", "RGBA"}:
        image = image.convert("RGBA" if "A" in image.mode else "RGB")
    return image


def find_best_image(asset: dict, max_candidates_per_page: int) -> tuple[dict | None, list[dict]]:
    attempts = []
    best = None

    for page in asset.get("candidatePages", []):
        page_url = page.get("url")
        if not page_url:
            continue
        try:
            body, content_type = fetch(page_url)
            if "html" not in content_type.lower() and not body.lstrip().startswith(b"<"):
                candidate_urls = [{"url": page_url, "score": 100, "reason": "direct image"}]
            else:
                html = body.decode("utf-8", errors="replace")
                candidate_urls = extract_candidate_urls(html, page_url, asset, page)
        except Exception as exc:
            attempts.append({"pageUrl": page_url, "status": "page_failed", "error": str(exc)})
            continue

        for candidate in candidate_urls[:max_candidates_per_page]:
            image_url = candidate["url"]
            try:
                image_body, image_content_type = fetch(image_url)
                if len(image_body) < 2048:
                    attempts.append({"imageUrl": image_url, "status": "rejected", "reason": "too_small_bytes"})
                    continue
                image = open_candidate_image(image_body)
                ok, quality, reject_reasons = image_quality_score(image)
                attempt = {
                    "imageUrl": image_url,
                    "sourcePageUrl": page_url,
                    "status": "accepted_candidate" if ok else "rejected",
                    "contentType": image_content_type,
                    "width": image.size[0],
                    "height": image.size[1],
                    "score": candidate["score"] + quality // 10_000,
                    "reasons": reject_reasons,
                    "reason": candidate["reason"],
                }
                attempts.append(attempt)
                if ok and (best is None or attempt["score"] > best["attempt"]["score"]):
                    best = {"attempt": attempt, "image": image}
            except Exception as exc:
                attempts.append({"imageUrl": image_url, "status": "failed", "error": str(exc)})

    return best, attempts


def make_contact_sheet(downloaded: list[dict], output_path: Path) -> None:
    if not downloaded:
        return

    thumb_w, thumb_h = 260, 190
    label_h = 56
    cols = 4
    rows = (len(downloaded) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * thumb_w, rows * (thumb_h + label_h)), "white")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()

    for index, item in enumerate(downloaded):
        row, col = divmod(index, cols)
        x = col * thumb_w
        y = row * (thumb_h + label_h)
        image_path = ROOT / item["file"]
        image = Image.open(image_path).convert("RGB")
        image.thumbnail((thumb_w - 20, thumb_h - 20), Image.Resampling.LANCZOS)
        ix = x + (thumb_w - image.width) // 2
        iy = y + (thumb_h - image.height) // 2
        sheet.paste(image, (ix, iy))
        draw.rectangle([x, y, x + thumb_w - 1, y + thumb_h + label_h - 1], outline=(220, 220, 220))
        label = f"{index + 1}. {item['assetKey']}"
        wrapped = [label[i : i + 34] for i in range(0, len(label), 34)]
        for line_index, line in enumerate(wrapped[:2]):
            draw.text((x + 8, y + thumb_h + 6 + line_index * 14), line, fill=(0, 0, 0), font=font)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path, "PNG")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--contact-sheet", type=Path, default=DEFAULT_CONTACT_SHEET)
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--delay", type=float, default=0.3)
    parser.add_argument("--include-review", action="store_true")
    parser.add_argument("--max-candidates-per-page", type=int, default=10)
    args = parser.parse_args()
    args.source = args.source.resolve()
    args.output_dir = args.output_dir.resolve()
    args.manifest = args.manifest.resolve()
    args.contact_sheet = args.contact_sheet.resolve()

    data = json.loads(args.source.read_text(encoding="utf-8"))
    assignments = {
        item["imageAssetKey"]: item for item in data.get("productImageAssignments", [])
    }
    assets = []
    for asset in data.get("imageAssets", []):
        assignment = assignments.get(asset["assetKey"], {})
        if not args.include_review:
            if asset.get("matchConfidence") != "high":
                continue
            if assignment.get("needsOperatorReview"):
                continue
            if assignment.get("assignmentConfidence") != "high":
                continue
        assets.append(asset)
    if args.limit:
        assets = assets[: args.limit]

    args.output_dir.mkdir(parents=True, exist_ok=True)
    results = []
    for asset in assets:
        best, attempts = find_best_image(asset, args.max_candidates_per_page)
        if best is None:
            result = {
                "assetKey": asset["assetKey"],
                "status": "failed",
                "matchConfidence": asset.get("matchConfidence"),
                "sourceRows": assignments.get(asset["assetKey"], {}).get("sourceRows", []),
                "attempts": attempts[-12:],
            }
            print(f"failed: {asset['assetKey']}")
        else:
            image = best["image"].convert("RGBA")
            target = args.output_dir / f"{slug(asset['assetKey'])}.png"
            image.save(target, "PNG", optimize=True)
            attempt = best["attempt"]
            result = {
                "assetKey": asset["assetKey"],
                "status": "downloaded_png",
                "file": str(target.relative_to(ROOT)).replace("\\", "/"),
                "sourceRows": assignments.get(asset["assetKey"], {}).get("sourceRows", []),
                "sourcePageUrl": attempt["sourcePageUrl"],
                "imageUrl": attempt["imageUrl"],
                "originalContentType": attempt["contentType"],
                "width": attempt["width"],
                "height": attempt["height"],
                "matchConfidence": asset.get("matchConfidence"),
                "rightsStatus": asset.get("rightsStatus"),
                "visualReviewStatus": "pending_manual_scan",
            }
            print(f"downloaded_png: {asset['assetKey']} -> {target.name}")
        results.append(result)
        time.sleep(args.delay)

    downloaded = [item for item in results if item["status"] == "downloaded_png"]
    make_contact_sheet(downloaded, args.contact_sheet)

    manifest = {
        "source": str(args.source.relative_to(ROOT)).replace("\\", "/"),
        "outputDir": str(args.output_dir.relative_to(ROOT)).replace("\\", "/"),
        "contactSheet": str(args.contact_sheet.relative_to(ROOT)).replace("\\", "/"),
        "filter": "high confidence and no operator review" if not args.include_review else "all assets",
        "totalAssetsAttempted": len(results),
        "downloadedPng": len(downloaded),
        "failed": sum(1 for item in results if item["status"] != "downloaded_png"),
        "rightsNote": data.get("batch", {}).get("rightsNote"),
        "items": results,
    }
    args.manifest.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
