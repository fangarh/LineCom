import json
import tempfile
import unittest
from contextlib import redirect_stdout
from io import BytesIO, StringIO
from pathlib import Path
from unittest.mock import patch

from PIL import Image

from tools import download_tktdf_product_images as downloader


def png_bytes() -> bytes:
    buffer = BytesIO()
    Image.new("RGB", (24, 16), "white").save(buffer, "PNG")
    return buffer.getvalue()


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
                with redirect_stdout(StringIO()):
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
                with redirect_stdout(StringIO()):
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
