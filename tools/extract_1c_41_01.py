import json
import zipfile
import xml.etree.ElementTree as ET
from argparse import ArgumentParser
from decimal import Decimal
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "Assets" / "1c_export.xlsx"
DEFAULT_OUTPUT = ROOT / "Assets" / "1c_export_41_01_nomenclature_by_category.json"
NS = {"a": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


CATEGORIES = {
    "twisted-pair-cable": ("Витая пара", True),
    "fiber-optic-cable": ("Оптический кабель", True),
    "fiber-optic-components": ("Оптические компоненты", True),
    "connectors-adapters": ("Разъемы и переходники", True),
    "patch-cords": ("Патч-корды", True),
    "telecom-cabinets-racks": ("Телекоммуникационные шкафы и 19-дюймовые аксессуары", False),
    "structured-cabling-components": ("СКС и медные компоненты", False),
    "coax-tv-components": ("Коаксиальные и ТВ-компоненты", False),
    "power-electrical": ("Электропитание и электротехника", False),
    "power-cable-wire": ("Силовой кабель и электропровод", False),
    "cable-management": ("Кабеленесущие системы и монтажные коробки", False),
    "mounting-hardware": ("Монтажный крепеж", False),
    "network-equipment": ("Активное сетевое оборудование", False),
    "tools-consumables": ("Инструменты и расходные материалы", False),
    "non-core": ("Непрофильные и служебные остатки", False),
}


def colnum(ref: str) -> int:
    n = 0
    for ch in "".join(ch for ch in ref if ch.isalpha()):
        n = n * 26 + ord(ch.upper()) - 64
    return n


def read_cell(cell: ET.Element, shared: list[str]):
    v = cell.find("a:v", NS)
    if v is None:
        return None

    raw = v.text or ""
    if cell.attrib.get("t") == "s":
        return shared[int(raw)]

    try:
        d = Decimal(raw)
        return int(d) if d == d.to_integral_value() else float(d)
    except Exception:
        return raw


def clean_name(value) -> str:
    return "" if value is None else " ".join(str(value).split()).strip()


def non_null_number(value):
    return value if isinstance(value, (int, float)) else None


def has_any(text: str, words: list[str]) -> bool:
    return any(word in text for word in words)


def classify(name: str):
    t = name.lower().replace("ё", "е")

    def pick(slug: str, *words: str, confidence: str = "high", review: bool = False):
        return slug, confidence, review, [word for word in words if word]

    if "патч-корд" in t or "патч корд" in t or "патчкорд" in t:
        return pick("patch-cords", "патч-корд")
    if "sfp" in t or "dac модуль" in t:
        return pick("fiber-optic-components", "sfp/dac")
    if "медиаконвертер" in t or "коммутатор" in t or "olt" in t:
        return pick("network-equipment", "медиаконвертер/коммутатор/olt")
    if "оптический приемник" in t:
        return pick("coax-tv-components", "оптический приемник rf", confidence="medium")

    is_cable = t.startswith("кабель ")
    if is_cable and has_any(t, ["utp", "ftp", "cat.5", "cat 5", "4x2", "4х2", "lappnet", "lanmax", "skynet"]):
        return pick("twisted-pair-cable", "utp/ftp/cat")
    if (is_cable or t.startswith("оптичесикй кабель") or t.startswith("оптический кабель")) and has_any(
        t, ["оптичес", "ftths", "adss", "окс", "омзк", "опд", "отц"]
    ):
        return pick("fiber-optic-cable", "оптический кабель")
    if is_cable and has_any(t, ["rg-6", "rg6", "sat 703", "коаксиал"]):
        return pick("coax-tv-components", "коаксиальный кабель")
    if is_cable and has_any(t, ["ввг", "вбш", "квбш", "кгвв", "кпс", "ксвв", "кспв"]):
        return pick("power-cable-wire", "силовой/контрольный кабель")
    if t.startswith("провод "):
        return pick("power-cable-wire", "провод")

    if has_any(
        t,
        [
            "кросс оптический",
            "муфта оптическая",
            "сплайс",
            "кдзс",
            "пигтейл",
            "делитель оптический",
            "разветвитель оптический",
            "аттенюатор",
            "адаптер sc/",
            "розетка оптическая",
            "оптическая розетка",
            "планка съемная на 8 портов sc",
            "комплект для защиты сварных соединений",
        ],
    ):
        return pick("fiber-optic-components", "оптическая пассивка")

    if has_any(
        t,
        [
            "ответвитель",
            "сплиттер sah",
            "усилитель terra",
            "усилитель телевизионный",
            "радиорозетка",
            "f-коннектор",
            "разъем f",
            "разъём р911",
            "разъём р912",
            "переходник угловой с f",
            "переходной разъем ff-ff",
            "преходник f",
            "переменный эквалайзер",
        ],
    ):
        return pick("coax-tv-components", "tv/coax")

    if has_any(t, ["rj-45", "rj45", "8p8c", "плинт", "соединительный бокс utp", "компьютерная внешняя", "коробка коммутационная"]):
        if has_any(t, ["коннектор", "соединительный модуль", "переходник"]):
            return pick("connectors-adapters", "rj45 connector")
        return pick("structured-cabling-components", "sks/rj45")
    if has_any(t, ["скотчлок", "uy2", "коннектор", "разъем ", "разъём ", "адаптер "]):
        return pick("connectors-adapters", "разъем/адаптер")

    if has_any(t, ["шкаф", "замок для пенального шкафа", "замок ригельный"]) and not has_any(t, ["щит щрн"]):
        return pick("telecom-cabinets-racks", "шкаф/замок")
    if has_any(
        t,
        [
            "аккумулятор",
            "аккумуляторная батарея",
            "ибп",
            "блок розеток",
            "вилка ",
            "выключатель автоматический",
            "колодка ",
            "контактор",
            "корпус пластиковый",
            "реле времени",
            "розетка 2 поста",
            "розетка двойная",
            "сетевой фильтр",
            "шина нулевая",
            "щит щрн",
            "электрический тройник",
        ],
    ):
        return pick("power-electrical", "электрика/питание")
    if has_any(
        t,
        [
            "кабель-канал",
            "кабельный канал",
            "труба гладкая",
            "труба гофрированная",
            "держатель с защ",
            "крепеж-клипса",
            "крепеж клипса",
            "коробка распределительная",
        ],
    ):
        return pick("cable-management", "кабеленесущие системы")
    if has_any(
        t,
        [
            "анкер",
            "зажим",
            "дюбель",
            "карабин",
            "крепеж",
            "крепежный набор",
            "крепёжный набор",
            "крепление для муфты",
            "лента крепежная",
            "перфолента",
            "саморез",
            "скоба ",
            "скрепа ",
            "стяжка",
            "стяжки",
            "талреп",
            "узел крепления",
            "усиленная площадка",
            "хомутная оцинкованная лента",
            "шуруп",
        ],
    ):
        return pick("mounting-hardware", "монтажный крепеж")
    if has_any(
        t,
        [
            "бур",
            "газовый баллон",
            "горелка",
            "дозатор для спирта",
            "индикаторная отвертка",
            "инструмент",
            "тестер",
            "кримпер",
            "узк",
            "мультиметр",
            "перчатки",
            "рюкзак для инструментов",
            "салфетки",
            "сжатый воздух",
            "спирт изопропиловый",
        ],
    ):
        return pick("tools-consumables", "инструмент/расходник")
    if has_any(t, ["бензин", "картридж", "складной стул"]):
        return pick("non-core", "непрофильный остаток")

    return "non-core", "low", True, []


def load_rows(source: Path):
    with zipfile.ZipFile(source) as z:
        shared = []
        root = ET.fromstring(z.read("xl/sharedStrings.xml"))
        for si in root.findall("a:si", NS):
            shared.append("".join(t.text or "" for t in si.findall(".//a:t", NS)))
        sheet = ET.fromstring(z.read("xl/worksheets/sheet1.xml"))

    rows = []
    in_section = False
    section_row = None
    section_totals = None
    for row in sheet.findall("a:sheetData/a:row", NS):
        r = int(row.attrib["r"])
        cells = {colnum(c.attrib["r"]): read_cell(c, shared) for c in row.findall("a:c", NS)}
        name = clean_name(cells.get(1))
        if name.startswith("41.01"):
            in_section = True
            section_row = r
            section_totals = {
                "sourceRow": r,
                "name": name,
                "quantity": non_null_number(cells.get(2)),
                "amount": non_null_number(cells.get(4)),
            }
            continue
        if not in_section:
            continue
        if name.startswith("Итого") or (len(name) >= 5 and name[0:2].isdigit() and name[2] == "."):
            break
        if not name:
            continue

        slug, confidence, review, matched = classify(name)
        rows.append(
            {
                "sourceRow": r,
                "name": name,
                "sourceAccount": "41.01",
                "quantity": non_null_number(cells.get(2)),
                "unitCost": non_null_number(cells.get(3)),
                "amount": non_null_number(cells.get(4)),
                "aging": {
                    "upTo90Days": {"quantity": non_null_number(cells.get(5)), "amount": non_null_number(cells.get(6))},
                    "days91To180": {"quantity": non_null_number(cells.get(7)), "amount": non_null_number(cells.get(8))},
                    "days181To365": {"quantity": non_null_number(cells.get(9)), "amount": non_null_number(cells.get(10))},
                    "days366To545": {"quantity": non_null_number(cells.get(11)), "amount": non_null_number(cells.get(12))},
                    "days546To730": {"quantity": non_null_number(cells.get(13)), "amount": non_null_number(cells.get(14))},
                    "over730Days": {"quantity": non_null_number(cells.get(15)), "amount": non_null_number(cells.get(16))},
                },
                "classification": {
                    "categorySlug": slug,
                    "categoryName": CATEGORIES[slug][0],
                    "confidence": confidence,
                    "matchedKeywords": matched,
                    "needsReview": bool(review or confidence != "high"),
                },
            }
        )

    return section_row, section_totals, rows


def main():
    parser = ArgumentParser(description="Extract and categorize 1C account 41.01 nomenclature from an XLSX report.")
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    section_row, section_totals, rows = load_rows(args.source)
    by_slug = {slug: [] for slug in CATEGORIES}
    for item in rows:
        by_slug[item["classification"]["categorySlug"]].append(item)

    result = {
        "source": {
            "file": str(args.source),
            "worksheet": "sheet1",
            "reportTitle": "Остатки товаров по срокам хранения на 07.05.2026",
        },
        "extraction": {
            "sourceAccount": "41.01",
            "sourceAccountName": "Товары на складах",
            "sourceAccountRow": section_row,
            "itemRowStart": rows[0]["sourceRow"] if rows else None,
            "itemRowEnd": rows[-1]["sourceRow"] if rows else None,
            "itemCount": len(rows),
            "classificationBasis": (
                "Категории LineCom из vault/Человекочитаемое + детализирующие категории "
                "для непрофильных/вспомогательных строк 1С; классификация по ключевым словам "
                "названия номенклатуры."
            ),
            "importantNote": (
                "В выгрузке 1С раздел 41.01 является плоским списком без вложенных категорий; "
                "categorySlug/categoryName являются проектной классификацией для предварительного "
                "импорта и требуют операторского подтверждения перед публикацией."
            ),
            "sectionTotals": section_totals,
        },
        "categories": [],
    }

    for slug, (name, core) in CATEGORIES.items():
        items = by_slug[slug]
        if items:
            result["categories"].append(
                {
                    "slug": slug,
                    "name": name,
                    "projectCoreCategory": core,
                    "itemCount": len(items),
                    "items": items,
                }
            )

    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"output: {args.output}")
    print(f"items: {len(rows)}")
    for category in result["categories"]:
        print(f"{category['slug']}: {category['itemCount']}")
    print(f"needsReview: {sum(1 for item in rows if item['classification']['needsReview'])}")


if __name__ == "__main__":
    main()
