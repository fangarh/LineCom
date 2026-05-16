import json
import tempfile
import unittest
from pathlib import Path

from tools import product_image_review_workflow as workflow


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
