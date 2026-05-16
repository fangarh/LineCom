import json
import tempfile
import unittest
from io import BytesIO
from pathlib import Path
from unittest.mock import patch

from PIL import Image

from tools import product_image_review_workflow as workflow


def make_png_bytes() -> bytes:
    buffer = BytesIO()
    Image.new("RGB", (32, 24), "white").save(buffer, "PNG")
    return buffer.getvalue()


class ProductImageReviewWorkflowTests(unittest.TestCase):
    def test_normalize_selection_keeps_first_two_selected_per_product(self) -> None:
        candidates = {
            "category": {"slug": "cable", "name": "\u041a\u0430\u0431\u0435\u043b\u044c"},
            "products": [
                {
                    "productId": "p1",
                    "externalId": "101",
                    "name": "\u041a\u0430\u0431\u0435\u043b\u044c UTP",
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
            "category": {"slug": "cable", "name": "\u041a\u0430\u0431\u0435\u043b\u044c"},
            "products": [
                {
                    "productId": "p1",
                    "externalId": "101",
                    "name": "\u041a\u0430\u0431\u0435\u043b\u044c UTP",
                    "candidates": [],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "selection.json"
            workflow.write_selection(path, workflow.normalize_selection(candidates, operator="codex"))

            data = json.loads(path.read_text(encoding="utf-8"))

        self.assertEqual("cable", data["category"]["slug"])
        self.assertEqual("codex", data["selectedByOperator"])

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
        self.assertIn('data-product-id="p1"', html)
        self.assertIn("downloadSelection", html)

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
