import { describe, expect, it } from "vitest";
import { describeHomepageTargetVisibility } from "./admin-homepage-visibility";

describe("describeHomepageTargetVisibility", () => {
  it("describes a visible published product", () => {
    expect(
      describeHomepageTargetVisibility({
        type: "product",
        isActive: true,
        publishStatus: "published",
        slug: "kabel",
        categoryName: "Кабели",
      }),
    ).toBe("Попадет на витрину");
  });

  it("describes why a product will not be visible", () => {
    expect(
      describeHomepageTargetVisibility({
        type: "product",
        isActive: false,
        publishStatus: "draft",
        slug: "",
        categoryName: "",
      }),
    ).toBe("Не попадет: товар неактивен, не опубликован, нет slug, нет категории");
  });

  it("describes why a category will not be visible", () => {
    expect(
      describeHomepageTargetVisibility({
        type: "category",
        isActive: false,
        slug: "",
        isVisibleInMenu: false,
      }),
    ).toBe("Не попадет: категория неактивна, нет slug, не в меню");
  });
});
