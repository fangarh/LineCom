import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";

export const metadata: Metadata = noindexPageMetadata("Cookie LineCom");

const cookieCategories = [
  {
    title: "Необходимые cookie",
    text:
      "Используются для базовой работы сайта: авторизация, защита форм, сохранение выбора cookie, корректная работа интерфейса и заявок. Эти cookie не отключаются через баннер.",
  },
  {
    title: "Аналитические cookie",
    text:
      "Помогают оценивать посещаемость страниц, ошибки и востребованность разделов каталога. Они включаются только после согласия пользователя.",
  },
  {
    title: "Маркетинговые cookie",
    text:
      "Могут использоваться для рекламных пикселей, ретаргетинга и оценки эффективности рекламных кампаний. Они включаются только после согласия пользователя.",
  },
  {
    title: "Функциональные cookie внешних сервисов",
    text:
      "Могут появляться при подключении карт, чатов, видео и других встраиваемых сервисов. Такие сервисы включаются только после согласия пользователя.",
  },
];

export default function CookiesPage() {
  return (
    <div className="content-page legal-page">
      <section className="content-hero legal-page__hero" aria-labelledby="cookies-title">
        <p className="eyebrow">Правовая информация</p>
        <h1 id="cookies-title">Использование cookie</h1>
        <p className="lead-text">
          На сайте LineCom применяются необходимые cookie и похожие технологии. Необязательные
          категории используются только после выбора пользователя в настройках cookie.
        </p>
      </section>

      <section className="legal-page__section" aria-labelledby="cookies-categories-title">
        <h2 id="cookies-categories-title">Категории</h2>
        <div className="legal-page__list">
          {cookieCategories.map((category) => (
            <article key={category.title} className="legal-page__item">
              <h3>{category.title}</h3>
              <p>{category.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="legal-page__section" aria-labelledby="cookies-control-title">
        <h2 id="cookies-control-title">Как изменить выбор</h2>
        <p>
          Пользователь может изменить согласие через ссылку «Настройки cookie» в нижней части сайта.
          Новый выбор заменяет предыдущий. Если состав cookie или цели обработки существенно изменятся,
          сайт запросит согласие повторно.
        </p>
      </section>
    </div>
  );
}
