import Link from "next/link";
import type { Metadata } from "next";
import { routes } from "@/lib/routes";
import { indexablePageMetadata } from "@/lib/seo/metadata";

export const metadata: Metadata = indexablePageMetadata({
  title: "Доставка LineCom",
  description:
    "LineCom организует доставку кабеля, оптических и сетевых компонентов по России и всему миру после согласования заявки.",
  canonicalPath: "/delivery",
});

const deliveryOptions = [
  {
    title: "По России",
    text: "Подберем транспортную компанию, самовывоз со склада партнера или адресную доставку под график объекта.",
  },
  {
    title: "Международно",
    text: "Согласуем маршрут, упаковку и документы для поставок за пределы России, включая авиа, авто и морские плечи.",
  },
  {
    title: "Под проект",
    text: "Разделим поставку на партии, учтем сроки монтажа и подготовим позиции так, чтобы их было удобно принять на объекте.",
  },
];

const deliverySteps = [
  "Вы отправляете заявку с позициями, количеством и адресом получения.",
  "Мы проверяем комплектность, габариты, доступность и возможные ограничения по перевозке.",
  "Согласуем маршрут, срок, стоимость доставки и формат документов.",
  "Передаем груз выбранным способом и остаемся на связи до получения.",
];

export default function DeliveryPage() {
  return (
    <div className="content-page delivery-page">
      <section className="delivery-hero" aria-labelledby="delivery-title">
        <div className="delivery-hero__content">
          <p className="eyebrow">Доставка по миру</p>
          <h1 id="delivery-title">Доставим кабель и сетевые компоненты по всему миру</h1>
          <p className="lead-text">
            LineCom организует получение и доставку по России и за рубежом: от коробки с патч-кордами
            до проектной партии кабеля, оптики, шкафов и монтажных расходников.
          </p>
          <div className="content-actions">
            <Link className="button button--primary" href={routes.contacts()}>
              Обратная связь
            </Link>
            <Link className="button button--secondary" href={routes.catalog()}>
              Выбрать позиции
            </Link>
          </div>
        </div>
        <div className="delivery-hero__visual">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src="https://images.pexels.com/photos/1427541/pexels-photo-1427541.jpeg?auto=compress&cs=tinysrgb&w=1600"
            alt="Контейнерный терминал для международной доставки грузов"
          />
          <div className="delivery-route-card" aria-label="Маршруты доставки LineCom">
            <span>Маршруты</span>
            <strong>Россия · СНГ · Европа · Азия · другие направления по запросу</strong>
          </div>
        </div>
      </section>

      <section className="delivery-options" aria-labelledby="delivery-options-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Формат отправки</p>
            <h2 id="delivery-options-title">Подбираем способ под груз, сроки и объект</h2>
          </div>
        </div>
        <div className="delivery-options__grid">
          {deliveryOptions.map((option) => (
            <article key={option.title} className="delivery-option-card">
              <h3>{option.title}</h3>
              <p>{option.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="delivery-showcase" aria-labelledby="delivery-showcase-title">
        <div className="delivery-showcase__media">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src="https://images.pexels.com/photos/4483775/pexels-photo-4483775.jpeg?auto=compress&cs=tinysrgb&w=1400"
            alt="Складская зона подготовки груза к отправке"
          />
        </div>
        <div className="delivery-showcase__content">
          <p className="eyebrow">Перед отгрузкой</p>
          <h2 id="delivery-showcase-title">Проверяем не только адрес, но и условия приемки</h2>
          <p>
            Для кабеля, оптических компонентов и 19-дюймового оборудования важны габариты, вес, упаковка,
            маркировка и график разгрузки. Мы заранее уточняем эти детали, чтобы поставка приехала без
            лишних согласований на месте.
          </p>
          <dl className="delivery-checklist">
            <div>
              <dt>Документы</dt>
              <dd>Счет, закрывающие документы и данные получателя</dd>
            </div>
            <div>
              <dt>Упаковка</dt>
              <dd>Защита бухт, коробок, оптики и оборудования</dd>
            </div>
            <div>
              <dt>Логистика</dt>
              <dd>ТК, адресная доставка, авиа, авто или морской маршрут</dd>
            </div>
          </dl>
        </div>
      </section>

      <section className="delivery-process" aria-labelledby="delivery-process-title">
        <div className="delivery-process__intro">
          <p className="eyebrow">Как это работает</p>
          <h2 id="delivery-process-title">Сначала заявка, затем точная логистика</h2>
          <p>
            Мы не прячем доставку в типовой тариф: для B2B-поставок точнее сначала понять состав,
            объем, адрес и срочность.
          </p>
        </div>
        <ol className="delivery-steps">
          {deliverySteps.map((step, index) => (
            <li key={step}>
              <span>{String(index + 1).padStart(2, "0")}</span>
              <p>{step}</p>
            </li>
          ))}
        </ol>
      </section>

      <section className="delivery-final" aria-labelledby="delivery-final-title">
        <div>
          <h2 id="delivery-final-title">Нужно отправить комплект в другой город или страну?</h2>
          <p>
            Соберите позиции из каталога или опишите задачу свободным текстом. Мы вернемся с вариантом
            поставки и доставки после проверки состава заявки.
          </p>
        </div>
        <Link className="button button--primary" href={routes.request()}>
          Открыть заявку
        </Link>
      </section>
    </div>
  );
}
