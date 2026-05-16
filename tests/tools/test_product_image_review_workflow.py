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
