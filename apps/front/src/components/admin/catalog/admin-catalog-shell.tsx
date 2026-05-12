"use client";

import { useState } from "react";
import { AdminCategoryManager } from "./admin-category-manager";

export type AdminCatalogSection = "products" | "categories" | "brands" | "attributes";

type AdminCatalogShellProps = {
  csrfToken?: string | null;
};

type CatalogTab = {
  id: AdminCatalogSection;
  label: string;
  description: string;
};

const catalogTabs: CatalogTab[] = [
  {
    id: "products",
    label: "Товары",
    description: "Список товаров, фильтры и массовые действия будут добавлены в следующих задачах.",
  },
  {
    id: "categories",
    label: "Категории",
    description: "Дерево категорий, видимость и SEO-настройки.",
  },
  {
    id: "brands",
    label: "Бренды",
    description: "Управление брендами и логотипами будет добавлено в следующих задачах.",
  },
  {
    id: "attributes",
    label: "Характеристики",
    description: "Менеджер характеристик и значений будет добавлен в следующих задачах.",
  },
];

export function AdminCatalogShell({ csrfToken = null }: AdminCatalogShellProps) {
  const [activeSection, setActiveSection] = useState<AdminCatalogSection>("products");
  const activeTab = catalogTabs.find((tab) => tab.id === activeSection) ?? catalogTabs[0];

  return (
    <section className="admin-catalog-shell account-section" aria-label="Администрирование каталога">
      <div className="admin-catalog-toolbar">
        <div>
          <p className="eyebrow">Админка</p>
          <h1>Каталог</h1>
        </div>
        <p className="admin-catalog-status">Раздел: {activeTab.label}</p>
      </div>

      <div className="admin-catalog-tabs" role="tablist" aria-label="Разделы каталога">
        {catalogTabs.map((tab) => {
          const tabId = getTabId(tab.id);
          const panelId = getPanelId(tab.id);
          const isActive = activeSection === tab.id;

          return (
            <button
              aria-controls={panelId}
              aria-selected={isActive}
              className="button button--ghost"
              id={tabId}
              key={tab.id}
              onClick={() => setActiveSection(tab.id)}
              role="tab"
              type="button"
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      <div className="admin-catalog-grid">
        {catalogTabs.map((tab) => {
          const isActive = activeSection === tab.id;

          return (
            <section
              aria-labelledby={getTabId(tab.id)}
              className="admin-catalog-panel"
              hidden={!isActive}
              id={getPanelId(tab.id)}
              key={tab.id}
              role="tabpanel"
            >
              {tab.id === "categories" && isActive ? (
                <AdminCategoryManager csrfToken={csrfToken} />
              ) : (
                <>
                  <div className="admin-catalog-table">
                    <h2>{tab.label}</h2>
                    <p>{tab.description}</p>
                  </div>
                  <div className="admin-catalog-form" aria-label={`Параметры раздела ${tab.label}`}>
                    <p className="admin-catalog-status">
                      Данные не загружались. Менеджер раздела появится позже.
                    </p>
                  </div>
                </>
              )}
            </section>
          );
        })}
      </div>
    </section>
  );
}

function getTabId(section: AdminCatalogSection) {
  return `admin-catalog-${section}-tab`;
}

function getPanelId(section: AdminCatalogSection) {
  return `admin-catalog-${section}-panel`;
}
