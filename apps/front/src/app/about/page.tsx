import Link from "next/link";
import type { Metadata } from "next";
import { routes } from "@/lib/routes";
import { indexablePageMetadata } from "@/lib/seo/metadata";

export const metadata: Metadata = indexablePageMetadata({
  title: "О LineCom",
  description:
    "ООО Лайнком поставляет кабель, оптические и телекоммуникационные компоненты для организаций и монтажных задач.",
  canonicalPath: "/about",
});

export default function AboutPage() {
  return (
    <div className="content-page about-page">
      <section className="about-hero" aria-labelledby="about-title">
        <div className="about-hero__content">
          <p className="eyebrow">ООО «Лайнком»</p>
          <h1 id="about-title">Кабель, оптика и телеком-компоненты для рабочих задач</h1>
          <p className="lead-text">
            LineCom помогает организациям собрать понятную заявку: подобрать кабель, СКС, оптические
            компоненты, шкафы и монтажные расходники под объект, поставку или сервисную задачу.
          </p>
          <div className="content-actions">
            <Link className="button button--primary" href={routes.catalog()}>
              Перейти в каталог
            </Link>
            <Link className="button button--secondary" href={routes.request()}>
              Собрать заявку
            </Link>
          </div>
        </div>
        <div className="about-hero__signal" aria-hidden="true">
          <span />
          <span />
          <span />
          <span />
        </div>
      </section>

      <section className="about-company" aria-labelledby="about-company-title">
        <div className="about-company__story">
          <p className="eyebrow">Чем занимаемся</p>
          <h2 id="about-company-title">Оптовая поставка электронного и телекоммуникационного оборудования</h2>
          <p>
            Компания зарегистрирована в Санкт-Петербурге и работает в направлении ОКВЭД 46.52:
            оптовая торговля электронным и телекоммуникационным оборудованием и его запасными частями.
            На сайте мы делаем процесс проще: каталог помогает начать подбор, а заявка фиксирует
            позиции, количество и комментарии для дальнейшего согласования.
          </p>
          <div className="about-company__links">
            <a
              className="button button--primary"
              href="https://yandex.ru/maps/org/laynkom/29441372789/?ll=30.243859%2C59.936165&z=17"
              rel="noreferrer"
              target="_blank"
            >
              Открыть на карте
            </a>
          </div>
        </div>

        <div className="about-company__facts" aria-label="Реквизиты ООО Лайнком">
          <dl>
            <div>
              <dt>Полное название</dt>
              <dd>ООО «ЛАЙНКОМ»</dd>
            </div>
            <div>
              <dt>ИНН / ОГРН</dt>
              <dd>7801724840 / 1237800078845</dd>
            </div>
            <div>
              <dt>Дата регистрации</dt>
              <dd>07.07.2023</dd>
            </div>
            <div>
              <dt>Адрес</dt>
              <dd>Санкт-Петербург, ул. Шевченко, д. 23, к. 1, литера А, офис 2-1</dd>
            </div>
          </dl>
        </div>
      </section>

      <section className="about-bank-details" aria-labelledby="about-bank-details-title">
        <div className="about-bank-details__intro">
          <h2 id="about-bank-details-title">Реквизиты для документов и оплаты</h2>
          <p>
            Данные приведены по карточке ООО «Лайнком». Перед оплатой счета сверяйте реквизиты
            с выставленным документом.
          </p>
        </div>

        <dl className="about-bank-details__list">
          <div>
            <dt>Полное название</dt>
            <dd>Общество с ограниченной ответственностью «Лайнком»</dd>
          </div>
          <div>
            <dt>ИНН / КПП</dt>
            <dd>7801724840 / 780101001</dd>
          </div>
          <div>
            <dt>ОГРН / ОКПО</dt>
            <dd>1237800078845 / 57574838</dd>
          </div>
          <div>
            <dt>Расчетный счет</dt>
            <dd>40702810220000022090</dd>
          </div>
          <div>
            <dt>Банк</dt>
            <dd>ООО «Банк Точка»</dd>
          </div>
          <div>
            <dt>БИК</dt>
            <dd>044525104</dd>
          </div>
          <div>
            <dt>Корреспондентский счет</dt>
            <dd>30101810745374525104</dd>
          </div>
          <div>
            <dt>Юридический и фактический адрес</dt>
            <dd>
              199406, Санкт-Петербург, вн. тер. г. Муниципальный округ Гавань, ул. Шевченко,
              дом 23, корпус 1, литера А, помещение 1-Н, офис 2-1
            </dd>
          </div>
          <div>
            <dt>Почтовый адрес</dt>
            <dd>199406, Санкт-Петербург, а/я 9, ООО «Лайнком»</dd>
          </div>
          <div>
            <dt>Контакты</dt>
            <dd>
              <a href="tel:+79313064350">+7 931 306-43-50</a>
              <br />
              <a href="mailto:Linecom.sup@gmail.com">Linecom.sup@gmail.com</a>
            </dd>
          </div>
          <div>
            <dt>Генеральный директор</dt>
            <dd>Лопатин А.В.</dd>
          </div>
        </dl>
      </section>
    </div>
  );
}
