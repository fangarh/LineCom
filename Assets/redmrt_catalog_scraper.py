#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Сканер каталога redmrt.ru.
Результат: JSON со всеми товарами, характеристиками и деревом категорий.
Цена, остатки/наличие и количество намеренно не сохраняются.

Установка:
  pip install requests beautifulsoup4 lxml
Запуск:
  python redmrt_catalog_scraper.py --base https://redmrt.ru --out redmrt_products.json
"""

from __future__ import annotations

import argparse
import json
import re
import time
from collections import deque
from dataclasses import dataclass
from typing import Dict, List, Optional, Set, Tuple
from urllib.parse import urljoin, urlparse, urlunparse

import requests
from bs4 import BeautifulSoup, Tag

SKIP_TEXT = {
    "купить", "быстрый заказ", "в избранное", "в сравнение", "нашли дешевле?",
    "доставка", "оплата", "отзывы", "вопросы и ответы", "описание", "характеристики",
}
PRICE_RE = re.compile(r"\b\d[\d\s]*(?:[.,]\d+)?\s*₽(?:/\S+)?\b", re.I)
STOCK_RE = re.compile(r"\b(на складе|нет в наличии|под заказ|количество|остаток|остатки)\b", re.I)

@dataclass
class Page:
    url: str
    soup: BeautifulSoup

class RedmrtScraper:
    def __init__(self, base_url: str, delay: float = 0.25, timeout: int = 25):
        self.base_url = base_url.rstrip("/") + "/"
        self.domain = urlparse(self.base_url).netloc
        self.delay = delay
        self.timeout = timeout
        self.session = requests.Session()
        self.session.headers.update({
            "User-Agent": "Mozilla/5.0 (compatible; CatalogExportBot/1.0; +local export)",
            "Accept-Language": "ru,en;q=0.8",
        })
        self.cache: Dict[str, Page] = {}

    def norm_url(self, url: str) -> str:
        u = urljoin(self.base_url, url)
        p = urlparse(u)
        p = p._replace(fragment="", query="")
        path = re.sub(r"/+$", "", p.path) or "/"
        return urlunparse((p.scheme, p.netloc, path, "", "", ""))

    def is_internal(self, url: str) -> bool:
        return urlparse(urljoin(self.base_url, url)).netloc == self.domain

    def fetch(self, url: str) -> Page:
        url = self.norm_url(url)
        if url in self.cache:
            return self.cache[url]
        time.sleep(self.delay)
        r = self.session.get(url, timeout=self.timeout)
        r.raise_for_status()
        soup = BeautifulSoup(r.text, "lxml")
        page = Page(url=url, soup=soup)
        self.cache[url] = page
        return page

    @staticmethod
    def clean(text: str) -> str:
        return re.sub(r"\s+", " ", text or "").strip()

    def get_menu_categories(self, soup: BeautifulSoup) -> List[Dict]:
        # Основной каталог на сайте расположен рядом с текстом "Каталог товаров".
        links: List[Tuple[str, str]] = []
        for a in soup.find_all("a", href=True):
            title = self.clean(a.get_text(" "))
            href = self.norm_url(a["href"])
            if not title or not self.is_internal(href):
                continue
            if title.lower() in {"главная", "показать все", "о компании", "доставка и оплата", "блог", "отзывы о магазине", "контакты", "корзина"}:
                continue
            # Берем только ссылки, которые выглядят как URL каталога/категории, исключая карточки позже по наличию товаров.
            if "/" in urlparse(href).path.strip("/") or urlparse(href).path.strip("/"):
                links.append((title, href))

        # Жестко фиксируем видимую структуру главного меню, чтобы сохранить дерево даже если верстка меняется.
        known = [
            ("Витая пара", "vitaya-para", [
                ("Для внутренней прокладки", "vitaya-para/dlya-vnutrennej-prokladki"),
                ("Для внешней прокладки", "vitaya-para/dlya-vneshnej-prokladki"),
            ]),
            ("Компоненты СКС", "komponenty-sks", [
                ("Патч-панели", "komponenty-sks/patch-paneli"),
                ("Патч-корды", "komponenty-sks/patch-kordy"),
                ("Розетки", "komponenty-sks/rozetki"),
                ("Коннекторы", "komponenty-sks/konnektory"),
                ("Скотчлоки/соединительные модули", "komponenty-sks/skotchloki-soedinitelnye-moduli"),
            ]),
            ("Оптический кабель", "opticheskij-kabel", [
                ("Оптический кабель FTTH", "opticheskij-kabel/opticheskij-kabel-ftth"),
                ("Оптический кабель самонесущий ADSS", "opticheskij-kabel/opticheskij-kabel-samonesushhij-adss"),
            ]),
            ("Оптические компоненты", "opticheskie-komponenty", [
                ("Абонентская розетка", "opticheskie-komponenty/abonentskaya-rozetka"),
                ("Быстрые коннекторы, соединители", "opticheskie-komponenty/bystrye-konnektory-soediniteli"),
                ("Гильзы КДЗС", "opticheskie-komponenty/gilzy-kdzs"),
            ]),
            ("Оптические трансиверы", "opticheskie-transivery", []),
            ("Медиаконвертеры", "mediakonvertery", []),
            ("Оптические муфты", "opticheskie-mufty", [
                ("Оптические кросс-муфты FTTH", "opticheskie-mufty/opticheskie-kross-mufty-ftth"),
            ]),
            ("Арматура ВОЛС", "armatura-vols", []),
            ("Коаксиальные кабели", "koaksialnye-kabeli", []),
            ("Телекоммуникационные шкафы", "telekommunikacionnye-shkafy", [
                ("Шкафы телекоммуникационные настенные", "telekommunikacionnye-shkafy/shkafy-telekommunikacionnye-nastennye"),
                ("Шкафы напольные телекоммуникационные", "telekommunikacionnye-shkafy/shkafy-napolnye-telekommunikacionnye"),
                ("Шкафы антивандальные", "telekommunikacionnye-shkafy/shkafy-antivandalnye"),
                ("Всепогодные шкафы", "telekommunikacionnye-shkafy/vsepogodnye-shkafy"),
            ]),
            ("Разъемы, переходы, штекеры для RG-КАБЕЛЯ", "razemy-perehody-shtekery-dlya-rg-kabelya", []),
            ("ТВ-делители", "tv-deliteli", []),
        ]
        return [
            {"name": n, "url": self.norm_url(slug), "children": [
                {"name": cn, "url": self.norm_url(cslug), "children": []} for cn, cslug in ch
            ]} for n, slug, ch in known
        ]

    def flatten_categories(self, tree: List[Dict], prefix: Optional[List[str]] = None) -> Dict[str, List[str]]:
        out: Dict[str, List[str]] = {}
        prefix = prefix or []
        for node in tree:
            path = prefix + [node["name"]]
            out[node["url"]] = path
            out.update(self.flatten_categories(node.get("children", []), path))
        return out

    def category_product_links(self, category_url: str) -> Set[str]:
        page = self.fetch(category_url)
        soup = page.soup
        links: Set[str] = set()
        # OpenCart обычно хранит карточки в product-layout/product-thumb.
        selectors = [".product-layout a[href]", ".product-thumb a[href]", ".product-item a[href]", "main a[href]", "#content a[href]"]
        for sel in selectors:
            for a in soup.select(sel):
                text = self.clean(a.get_text(" "))
                href = self.norm_url(a.get("href", ""))
                if not text or not self.is_internal(href):
                    continue
                if href == category_url or href.startswith(category_url + "?--"):
                    continue
                # Товарная карточка обычно глубже категории и не является служебной ссылкой.
                if href.startswith(category_url + "/") and text.lower() not in SKIP_TEXT:
                    links.add(href)
        return links

    def breadcrumbs(self, soup: BeautifulSoup) -> List[str]:
        crumbs = []
        for sel in [".breadcrumb a", "ul.breadcrumb a", "nav[aria-label*=breadcrumb] a"]:
            for a in soup.select(sel):
                t = self.clean(a.get_text(" "))
                if t and t.lower() != "главная":
                    crumbs.append(t)
            if crumbs:
                break
        return crumbs

    def product_title(self, soup: BeautifulSoup) -> str:
        h1 = soup.find("h1")
        return self.clean(h1.get_text(" ")) if h1 else ""

    def product_code(self, soup: BeautifulSoup) -> str:
        text = self.clean(soup.get_text(" "))
        m = re.search(r"Код товара:\s*(.*?)(?:\s{2,}|\s*\d[\d\s]*\s*₽| Купить| Быстрый заказ)", text, re.I)
        return self.clean(m.group(1)) if m else ""

    def extract_characteristics(self, soup: BeautifulSoup) -> Dict[str, str]:
        specs: Dict[str, str] = {}
        # 1) Таблицы характеристик.
        for table in soup.find_all("table"):
            txt = self.clean(table.get_text(" ")).lower()
            if "характерист" not in txt and len(table.find_all("tr")) < 2:
                continue
            for tr in table.find_all("tr"):
                cells = [self.clean(c.get_text(" ")) for c in tr.find_all(["td", "th"])]
                cells = [c for c in cells if c]
                if len(cells) >= 2 and not PRICE_RE.search(" ".join(cells)) and not STOCK_RE.search(cells[0]):
                    specs[cells[0]] = cells[1]
        # 2) Definition list.
        for dl in soup.find_all("dl"):
            dts = dl.find_all("dt")
            dds = dl.find_all("dd")
            for dt, dd in zip(dts, dds):
                k = self.clean(dt.get_text(" "))
                v = self.clean(dd.get_text(" "))
                if k and v and not PRICE_RE.search(v) and not STOCK_RE.search(k):
                    specs[k] = v
        # 3) Fallback для страниц, где web-текст показывает характеристики строками.
        all_text = [self.clean(x) for x in soup.get_text("\n").split("\n")]
        all_text = [x for x in all_text if x]
        if len(specs) < 2 and "Характеристики" in all_text:
            idxs = [i for i, x in enumerate(all_text) if x == "Характеристики"]
            start = idxs[-1] + 1 if idxs else 0
            stop_words = {"Отзывы", "Оставить отзыв", "Вопросы и ответы", "Доставка", "Оплата"}
            lines = []
            for x in all_text[start:start + 80]:
                if x in stop_words:
                    break
                if not PRICE_RE.search(x) and not STOCK_RE.search(x):
                    lines.append(x)
            # Вариант: ключ и значение идут отдельными строками или одной строкой "Ключ Значение".
            common_keys = [
                "Серия", "Применение", "Маркировка жил", "Цвет внешн. оболочки", "Категория (Cat)",
                "Материал проводника", "Экран поверх скрутки", "Длина изделия", "Марка кабеля провода",
                "Не поддерживает горения", "Низкое дымовыделение", "Не содержит галогенов", "Тип", "Материал",
                "Цвет", "Размер", "Высота", "Ширина", "Глубина", "Количество портов", "Категория",
                "Производитель", "Бренд", "Артикул", "Модель",
            ]
            for line in lines:
                for key in sorted(common_keys, key=len, reverse=True):
                    if line.startswith(key + " "):
                        val = self.clean(line[len(key):])
                        if val:
                            specs[key] = val
                            break
        return specs

    def description(self, soup: BeautifulSoup) -> str:
        parts = []
        for sel in ["#tab-description", ".tab-description", "[id*=description]", ".product-description"]:
            el = soup.select_one(sel)
            if el:
                text = self.clean(el.get_text(" "))
                if text and not PRICE_RE.search(text):
                    parts.append(text)
                    break
        return parts[0] if parts else ""

    def parse_product(self, url: str, fallback_category_path: Optional[List[str]] = None) -> Dict:
        page = self.fetch(url)
        soup = page.soup
        title = self.product_title(soup)
        crumbs = self.breadcrumbs(soup)
        category_path = [c for c in crumbs if c != title]
        if not category_path and fallback_category_path:
            category_path = fallback_category_path
        item = {
            "name": title,
            "url": page.url,
            "code": self.product_code(soup),
            "category_path": category_path,
            "characteristics": self.extract_characteristics(soup),
            "description": self.description(soup),
        }
        # Удаляем пустые поля.
        return {k: v for k, v in item.items() if v not in ("", [], {}, None)}

    def run(self) -> Dict:
        home = self.fetch(self.base_url)
        tree = self.get_menu_categories(home.soup)
        cat_paths = self.flatten_categories(tree)
        product_to_cat: Dict[str, List[str]] = {}
        category_stats = []
        for cat_url, path in cat_paths.items():
            try:
                links = self.category_product_links(cat_url)
            except Exception as e:
                category_stats.append({"url": cat_url, "path": path, "error": str(e)})
                continue
            for link in links:
                # Если товар найден в дочерней категории, оставляем более глубокий путь.
                if link not in product_to_cat or len(path) > len(product_to_cat[link]):
                    product_to_cat[link] = path
            category_stats.append({"url": cat_url, "path": path, "products_found": len(links)})
        products = []
        for i, (url, path) in enumerate(sorted(product_to_cat.items()), 1):
            try:
                products.append(self.parse_product(url, path))
                print(f"[{i}/{len(product_to_cat)}] OK {url}")
            except Exception as e:
                products.append({"url": url, "category_path": path, "error": str(e)})
                print(f"[{i}/{len(product_to_cat)}] ERROR {url}: {e}")
        return {
            "source": self.base_url.rstrip("/"),
            "export_note": "Цены, остатки и количество исключены из выгрузки.",
            "categories": tree,
            "category_scan_stats": category_stats,
            "products_count": len(products),
            "products": products,
        }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default="https://redmrt.ru")
    parser.add_argument("--out", default="redmrt_products.json")
    parser.add_argument("--delay", type=float, default=0.25)
    args = parser.parse_args()

    scraper = RedmrtScraper(args.base, delay=args.delay)
    data = scraper.run()
    with open(args.out, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    print(f"Saved: {args.out}; products: {data['products_count']}")

if __name__ == "__main__":
    main()
