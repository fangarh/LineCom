import Link from "next/link";
import { routes } from "@/lib/routes";

export const metadata = {
  title: "Доставка LineCom",
  description: "Доставка и получение кабеля, оптических и сетевых компонентов LineCom уточняются в процессе заявки.",
};

export default function DeliveryPage() {
  return (
    <div className="content-page">
      <section className="content-hero" aria-labelledby="delivery-title">
        <p className="eyebrow">Доставка</p>
        <h1 id="delivery-title">Согласуем удобный способ получения после подбора заявки</h1>
        <p className="lead-text">
          Выберите позиции или опишите задачу, отправьте заявку, а мы уточним комплектность, сроки
          и удобный формат передачи материалов для вашей организации.
        </p>
        <div className="content-actions">
          <Link className="button button--primary" href={routes.catalog()}>
            Выбрать позиции
          </Link>
          <Link className="button button--secondary" href={routes.request()}>
            Открыть заявку
          </Link>
        </div>
      </section>
    </div>
  );
}
