import Link from "next/link";
import type { Metadata } from "next";
import { routes } from "@/lib/routes";
import { indexablePageMetadata } from "@/lib/seo/metadata";

export const metadata: Metadata = indexablePageMetadata({
  title: "О LineCom",
  description: "LineCom помогает организациям подбирать кабель, оптические и сетевые компоненты под монтажные задачи.",
  canonicalPath: "/about",
});

export default function AboutPage() {
  return (
    <div className="content-page">
      <section className="content-hero" aria-labelledby="about-title">
        <p className="eyebrow">LineCom</p>
        <h1 id="about-title">Подбираем кабель и сетевые компоненты под реальные задачи</h1>
        <p className="lead-text">
          LineCom работает с организациями, которым нужен понятный B2B-процесс: выбрать позиции,
          описать задачу, уточнить комплектность и отправить заявку без публичной корзины и лишней рутины.
        </p>
        <div className="content-actions">
          <Link className="button button--primary" href={routes.catalog()}>
            Перейти в каталог
          </Link>
          <Link className="button button--secondary" href={routes.request()}>
            Собрать заявку
          </Link>
        </div>
      </section>
    </div>
  );
}
