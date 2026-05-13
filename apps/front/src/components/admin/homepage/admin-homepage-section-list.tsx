import type { AdminHomepageSection } from "@/lib/api/admin-homepage";

type AdminHomepageSectionListProps = {
  activeSectionId: string | null;
  isLoading: boolean;
  sections: AdminHomepageSection[];
  onSelect: (section: AdminHomepageSection) => void;
};

export function AdminHomepageSectionList({ activeSectionId, isLoading, sections, onSelect }: AdminHomepageSectionListProps) {
  return (
    <section className="admin-catalog-table admin-homepage-section" aria-label="Секции главной страницы">
      <div className="admin-category-manager__head">
        <h2>Секции</h2>
        {isLoading ? <p className="admin-catalog-status">Загрузка...</p> : null}
      </div>

      <div className="admin-category-manager__rows">
        {sections.map((section) => (
          <button
            aria-pressed={activeSectionId === section.id}
            className="admin-category-row"
            key={section.id}
            onClick={() => onSelect(section)}
            type="button"
          >
            <span>
              <strong>{section.title}</strong>
              <small>{section.code}</small>
            </span>
            <span className="admin-category-row__meta">
              {section.isActive ? "Активна" : "Скрыта"} · {section.items.length}/{section.itemLimit}
            </span>
          </button>
        ))}
      </div>
    </section>
  );
}
