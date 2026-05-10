#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Полный сканер каталога redmrt.ru.

Что выгружает:
- все найденные товары;
- url, название, код/артикул, описание;
- все характеристики товара;
- дерево категорий из хлебных крошек;
- category_path для каждого товара.

Что НЕ выгружает:
- цены;
- остатки/наличие;
- количество.

Установка:
  py -m pip install requests beautifulsoup4 lxml

Запуск:
  py redmrt_catalog_scraper_full.py --out redmrt_products.json

Если сайт режет частые запросы:
  py redmrt_catalog_scraper_full.py --delay 0.7 --out redmrt_products.json
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import time
from collections import deque
from dataclasses import dataclass
from typing import Dict, Iterable, List, Optional, Set, Tuple
from urllib.parse import parse_qsl, urlencode, urljoin, urlparse, urlunparse
from xml.etree import ElementTree as ET

import requests
from bs4 import BeautifulSoup, Tag

PRICE_RE = re.compile(r"\b\d[\d\s]*(?:[.,]\d+)?\s*(?:₽|руб\.?|р\.)\b", re.I)
STOCK_RE = re.compile(r"\b(на складе|нет в наличии|под заказ|количество|остаток|остатки|наличи[ея])\b", re.I)
BAD_COMMERCE_RE = re.compile(
    r"\b(купить|корзин[аы]|быстрый заказ|отправить заказ|доставка|оплат[аы]|кассовый чек|товарный чек|"
    r"обратный звонок|персональн[а-яё ]+данн|политика конфиденциальности|интернет-магазин)\b",
    re.I,
)
BAD_PATH_RE = re.compile(
    r"/(?:cart|checkout|account|create-account|forgot-password|login|logout|register|wishlist|compare|search|admin|api|storage|image|catalog/view|system)/?",
    re.I,
)
BAD_QUERY_KEYS = {
    "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "yclid", "gclid", "fbclid",
}
KEEP_QUERY_KEYS = {
    # product_id — товарные страницы OpenCart
    "product_id",
    # path/page/limit/sort/order/filter нужны для категорий и пагинации
    "path", "page", "limit", "sort", "order", "filter", "route",
}
SKIP_LINK_TEXT = {
    "", "главная", "купить", "быстрый заказ", "в избранное", "в сравнение", "сравнить", "закладки",
    "показать все", "читать далее", "подписаться", "отправить", "назад", "вперед", "доставка", "оплата",
    "отзывы", "вопросы и ответы", "описание", "характеристики", "контакты", "корзина",
}
STOP_SPEC_WORDS = {
    "Описание", "Отзывы", "Оставить отзыв", "Вопросы и ответы", "Доставка", "Оплата", "Рекомендуем",
    "Вы смотрели", "Похожие товары", "Популярные товары", "Купить", "Быстрый заказ",
}

@dataclass
class FetchResult:
    url: str
    status: int
    soup: Optional[BeautifulSoup]
    text: str

class FullRedmrtScraper:
    def __init__(self, base: str, out: str, delay: float, timeout: int, max_pages: int, debug: bool = False):
        self.base = base.rstrip("/") + "/"
        self.domain = urlparse(self.base).netloc.lower()
        self.out = out
        self.delay = delay
        self.timeout = timeout
        self.max_pages = max_pages
        self.debug = debug
        self.session = requests.Session()
        self.session.headers.update({
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
                          "(KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            "Accept-Language": "ru-RU,ru;q=0.9,en;q=0.7",
            "Connection": "keep-alive",
        })
        self.cache: Dict[str, FetchResult] = {}
        self.products: Dict[str, Dict] = {}
        self.category_paths: Dict[str, List[str]] = {}
        self.visited: Set[str] = set()
        self.errors: List[Dict] = []

    @staticmethod
    def clean(text: str) -> str:
        return re.sub(r"\s+", " ", text or "").strip()

    def norm_url(self, url: str) -> Optional[str]:
        if not url:
            return None
        if url.startswith(("mailto:", "tel:", "javascript:", "#")):
            return None
        abs_url = urljoin(self.base, url)
        p = urlparse(abs_url)
        if p.scheme not in {"http", "https"}:
            return None
        if p.netloc.lower() != self.domain:
            return None
        if BAD_PATH_RE.search(p.path):
            return None
        # отсекаем файлы и картинки
        if re.search(r"\.(?:jpg|jpeg|png|gif|webp|svg|ico|css|js|pdf|docx?|xlsx?|zip|rar)(?:$|\?)", p.path, re.I):
            return None
        path = re.sub(r"/{2,}", "/", p.path)
        path = re.sub(r"/+$", "", path) or "/"
        keep: List[Tuple[str, str]] = []
        for k, v in parse_qsl(p.query, keep_blank_values=True):
            kl = k.lower()
            if kl in BAD_QUERY_KEYS:
                continue
            if kl in KEEP_QUERY_KEYS:
                keep.append((kl, v))
        # стабильный порядок query, чтобы не плодить дубликаты
        keep.sort()
        query = urlencode(keep, doseq=True)
        return urlunparse(("https", self.domain, path, "", query, ""))

    def fetch(self, url: str) -> FetchResult:
        nurl = self.norm_url(url) or url
        if nurl in self.cache:
            return self.cache[nurl]
        time.sleep(self.delay)
        try:
            r = self.session.get(nurl, timeout=self.timeout, allow_redirects=True)
            final_url = self.norm_url(r.url) or nurl
            content_type = r.headers.get("content-type", "")
            text = r.text if "text" in content_type or "html" in content_type or "xml" in content_type else ""
            soup = BeautifulSoup(text, "lxml") if "html" in content_type or "text/html" in content_type or "" == content_type else None
            res = FetchResult(final_url, r.status_code, soup, text)
            self.cache[nurl] = res
            self.cache[final_url] = res
            return res
        except Exception as e:
            self.errors.append({"url": nurl, "error": str(e)})
            res = FetchResult(nurl, 0, None, "")
            self.cache[nurl] = res
            return res

    def sitemap_urls(self) -> Set[str]:
        urls: Set[str] = set()
        for sm in ("sitemap.xml", "sitemap_index.xml"):
            u = urljoin(self.base, sm)
            try:
                r = self.session.get(u, timeout=self.timeout)
                if r.status_code != 200 or "<" not in r.text:
                    continue
                root = ET.fromstring(r.text.encode("utf-8"))
                for loc in root.iter():
                    if loc.tag.endswith("loc") and loc.text:
                        nu = self.norm_url(loc.text.strip())
                        if nu:
                            urls.add(nu)
            except Exception:
                pass
        return urls

    def extract_links(self, soup: BeautifulSoup) -> Set[str]:
        out: Set[str] = set()
        # обычные ссылки
        for a in soup.find_all("a", href=True):
            txt = self.clean(a.get_text(" ")).lower()
            href = a.get("href", "")
            nu = self.norm_url(href)
            if nu and txt not in SKIP_LINK_TEXT:
                out.add(nu)
        # ссылки в data-атрибутах иногда используются в карточках/пагинации
        for tag in soup.find_all(True):
            for attr in ("data-href", "data-url", "data-link"):
                if tag.has_attr(attr):
                    nu = self.norm_url(str(tag.get(attr)))
                    if nu:
                        out.add(nu)
        return out

    def breadcrumbs(self, soup: BeautifulSoup) -> List[str]:
        selectors = [".breadcrumb a", "ul.breadcrumb a", "nav[aria-label*=breadcrumb] a", ".breadcrumbs a"]
        crumbs: List[str] = []
        for sel in selectors:
            found = []
            for a in soup.select(sel):
                t = self.clean(a.get_text(" "))
                if t and t.lower() != "главная":
                    found.append(t)
            if found:
                crumbs = found
                break
        return crumbs

    def title(self, soup: BeautifulSoup) -> str:
        h1 = soup.find("h1")
        if h1:
            return self.clean(h1.get_text(" "))
        og = soup.find("meta", property="og:title")
        return self.clean(og.get("content", "")) if og else ""

    def is_product_url(self, url: str) -> bool:
        p = urlparse(url)
        return any(k == "product_id" and v for k, v in parse_qsl(p.query))

    def looks_like_product_page(self, url: str, soup: BeautifulSoup) -> bool:
        if self.is_product_url(url):
            return True
        text = self.clean(soup.get_text(" ")).lower()
        has_buy = "купить" in text or "быстрый заказ" in text
        has_specs = "характеристики" in text or "технические характеристики" in text
        has_product_schema = bool(soup.select('[itemtype*="Product"], [type="application/ld+json"]'))
        return bool(self.title(soup) and (has_product_schema or (has_buy and has_specs)))

    def product_code(self, soup: BeautifulSoup) -> str:
        text = self.clean(soup.get_text(" "))
        patterns = [
            r"Код товара\s*[:№]?\s*([^\n|]+?)(?:\s{2,}| Купить| Быстрый заказ| В закладки|$)",
            r"Артикул\s*[:№]?\s*([^\n|]+?)(?:\s{2,}| Купить| Быстрый заказ|$)",
            r"Модель\s*[:№]?\s*([^\n|]+?)(?:\s{2,}| Купить| Быстрый заказ|$)",
        ]
        for pat in patterns:
            m = re.search(pat, text, re.I)
            if m:
                val = self.clean(m.group(1))
                val = PRICE_RE.sub("", val)
                if BAD_COMMERCE_RE.search(val):
                    return ""
                return self.clean(val)
        return ""

    def extract_characteristics(self, soup: BeautifulSoup) -> Dict[str, str]:
        specs: Dict[str, str] = {}

        def add(k: str, v: str):
            k = self.clean(k).rstrip(".:·—-")
            v = self.clean(v)
            if not k or not v or k == v:
                return
            if len(k) > 100 or len(v) > 300:
                return
            if PRICE_RE.search(v) or STOCK_RE.search(k) or STOCK_RE.search(v):
                return
            if BAD_COMMERCE_RE.search(k) or BAD_COMMERCE_RE.search(v):
                return
            if re.search(r"описание|отзывы|вопросы|купить|заказ|доставка|оплата", k, re.I):
                return
            specs[k] = v

        # Таблицы
        for table in soup.find_all("table"):
            for tr in table.find_all("tr"):
                cells = [self.clean(c.get_text(" ")) for c in tr.find_all(["td", "th"])]
                cells = [c for c in cells if c]
                if len(cells) >= 2:
                    add(cells[0], cells[-1])

        # Контейнеры около вкладки характеристик
        roots: List[Tag] = []
        roots.extend(soup.select("#tab-specification, #tab-characteristic, #tab-characteristics, [id*=character], [class*=character], [class*=specif], [class*=tab-pane]"))
        for node in soup.find_all(string=re.compile(r"Технические характеристики|Характеристики", re.I)):
            p = node.parent
            for _ in range(5):
                if isinstance(p, Tag) and p not in roots:
                    roots.append(p)
                p = p.parent if isinstance(p, Tag) else None

        for root in roots:
            for row in root.find_all(["li", "tr", "div", "p"], recursive=True):
                direct = [self.clean(ch.get_text(" ")) for ch in row.find_all(recursive=False) if isinstance(ch, Tag)]
                direct = [x for x in direct if x and x not in STOP_SPEC_WORDS and x.lower() not in SKIP_LINK_TEXT]
                if len(direct) >= 2:
                    add(direct[0], direct[-1])
                    continue
                txt = self.clean(row.get_text(" | "))
                if "|" in txt:
                    parts = [self.clean(x) for x in txt.split("|") if self.clean(x)]
                    # Удаляем дубли, часто возникающие из вложенных div/span
                    compact: List[str] = []
                    for part in parts:
                        if part not in compact and part not in STOP_SPEC_WORDS and part.lower() not in SKIP_LINK_TEXT:
                            compact.append(part)
                    if len(compact) >= 2:
                        add(compact[0], compact[-1])

        # Definition list
        for dl in soup.find_all("dl"):
            dts = dl.find_all("dt")
            dds = dl.find_all("dd")
            for dt, dd in zip(dts, dds):
                add(dt.get_text(" "), dd.get_text(" "))

        # Fallback: строки после заголовка Характеристики.
        if len(specs) < 2:
            lines = [self.clean(x) for x in soup.get_text("\n").split("\n")]
            lines = [x for x in lines if x]
            idxs = [i for i, x in enumerate(lines) if x.lower() in {"характеристики", "технические характеристики"}]
            if idxs:
                start = idxs[-1] + 1
                chunk: List[str] = []
                for x in lines[start:start + 120]:
                    if x in STOP_SPEC_WORDS:
                        break
                    if not PRICE_RE.search(x) and not STOCK_RE.search(x):
                        chunk.append(x)
                # На redmrt часто ключ и значение идут двумя соседними строками.
                for i in range(0, len(chunk) - 1, 2):
                    add(chunk[i], chunk[i + 1])

        return specs

    def description(self, soup: BeautifulSoup) -> str:
        for sel in ["#tab-description", ".tab-description", "[id*=description]", ".product-description", "[class*=description]"]:
            el = soup.select_one(sel)
            if el:
                txt = self.clean(el.get_text(" "))
                txt = PRICE_RE.sub("", txt)
                txt = re.sub(r"\bКупить\b.*$", "", txt, flags=re.I)
                if len(txt) > 20:
                    return self.clean(txt)
        return ""

    def product_id_from_url(self, url: str) -> str:
        for k, v in parse_qsl(urlparse(url).query):
            if k == "product_id":
                return v
        return ""

    def parse_product(self, url: str, soup: BeautifulSoup) -> Dict:
        name = self.title(soup)
        crumbs = self.breadcrumbs(soup)
        category_path = [c for c in crumbs if c != name]
        if category_path:
            self.category_paths[url] = category_path
        item = {
            "name": name,
            "url": url,
            "product_id": self.product_id_from_url(url),
            "code": self.product_code(soup),
            "category_path": category_path,
            "characteristics": self.extract_characteristics(soup),
            "description": self.description(soup),
        }
        return {k: v for k, v in item.items() if v not in ("", [], {}, None)}

    def build_category_tree(self, products: Iterable[Dict]) -> List[Dict]:
        root: Dict[str, Dict] = {}
        for p in products:
            path = p.get("category_path") or []
            cur = root
            for name in path:
                cur = cur.setdefault(name, {})

        def conv(node: Dict[str, Dict]) -> List[Dict]:
            return [{"name": name, "children": conv(child)} for name, child in sorted(node.items())]
        return conv(root)

    def crawl(self) -> Dict:
        queue: deque[str] = deque([self.base])
        sitemap = self.sitemap_urls()
        # sitemap добавляем в очередь: часто там есть все товары, включая те, что не видны с первой страницы категорий
        for u in sorted(sitemap):
            queue.append(u)

        page_count = 0
        while queue and page_count < self.max_pages:
            url = queue.popleft()
            url = self.norm_url(url) or url
            if url in self.visited:
                continue
            self.visited.add(url)
            res = self.fetch(url)
            if res.status >= 400 or not res.soup:
                if res.status >= 400:
                    self.errors.append({"url": url, "status": res.status})
                continue
            page_count += 1

            soup = res.soup
            final_url = res.url
            if self.looks_like_product_page(final_url, soup):
                item = self.parse_product(final_url, soup)
                key = item.get("product_id") or final_url
                # Если один товар встречается в нескольких категориях, оставляем более полный вариант.
                old = self.products.get(key)
                if not old or len(item.get("characteristics", {})) >= len(old.get("characteristics", {})):
                    self.products[key] = item
                print(f"PRODUCT {len(self.products):5d}: {item.get('name','')} | {final_url}")
            else:
                crumbs = self.breadcrumbs(soup)
                if crumbs:
                    self.category_paths[final_url] = crumbs
                if self.debug:
                    print(f"PAGE    {page_count:5d}: {final_url}")

            for link in self.extract_links(soup):
                if link not in self.visited:
                    queue.append(link)

            # Автогенерация следующих страниц категории, если есть текущая page=N или виден блок пагинации.
            # Это помогает, если пагинация отрисована не всеми ссылками.
            if not self.is_product_url(final_url):
                text = self.clean(soup.get_text(" ")).lower()
                if "показано" in text or "страниц" in text or soup.select(".pagination a"):
                    p = urlparse(final_url)
                    q = dict(parse_qsl(p.query))
                    current = int(q.get("page", "1") or "1")
                    for nxt in range(current + 1, current + 4):
                        q2 = q.copy(); q2["page"] = str(nxt)
                        next_url = urlunparse((p.scheme, p.netloc, p.path, "", urlencode(sorted(q2.items())), ""))
                        nu = self.norm_url(next_url)
                        if nu and nu not in self.visited:
                            queue.append(nu)

        products = list(self.products.values())
        products.sort(key=lambda x: (" / ".join(x.get("category_path", [])), x.get("name", "")))
        return {
            "source": self.base.rstrip("/"),
            "export_note": "Цены, остатки/наличие и количество исключены из выгрузки.",
            "scanned_pages": page_count,
            "visited_urls": len(self.visited),
            "products_count": len(products),
            "categories": self.build_category_tree(products),
            "errors": self.errors,
            "products": products,
        }

    def run(self):
        data = self.crawl()
        with open(self.out, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print("\nГотово")
        print(f"Файл: {self.out}")
        print(f"Товаров: {data['products_count']}")
        print(f"Просканировано страниц: {data['scanned_pages']}")
        if self.errors:
            print(f"Ошибок/пропусков: {len(self.errors)}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="https://redmrt.ru")
    ap.add_argument("--out", default="redmrt_products.json")
    ap.add_argument("--delay", type=float, default=0.25)
    ap.add_argument("--timeout", type=int, default=25)
    ap.add_argument("--max-pages", type=int, default=20000)
    ap.add_argument("--debug", action="store_true")
    args = ap.parse_args()
    try:
        FullRedmrtScraper(args.base, args.out, args.delay, args.timeout, args.max_pages, args.debug).run()
    except KeyboardInterrupt:
        print("\nОстановлено пользователем", file=sys.stderr)

if __name__ == "__main__":
    main()
