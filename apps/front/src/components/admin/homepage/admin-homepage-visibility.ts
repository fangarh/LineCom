export type HomepageTargetVisibilityInput =
  | {
      type: "product";
      isActive: boolean;
      publishStatus: string;
      slug: string | null;
      categoryName: string | null;
    }
  | {
      type: "category";
      isActive: boolean;
      slug: string | null;
      isVisibleInMenu: boolean;
    };

export function describeHomepageTargetVisibility(input: HomepageTargetVisibilityInput) {
  const reasons = input.type === "product" ? getProductVisibilityReasons(input) : getCategoryVisibilityReasons(input);

  if (reasons.length === 0) {
    return "Попадет на витрину";
  }

  return `Не попадет: ${reasons.join(", ")}`;
}

function getProductVisibilityReasons(input: Extract<HomepageTargetVisibilityInput, { type: "product" }>) {
  const reasons: string[] = [];

  if (!input.isActive) {
    reasons.push("товар неактивен");
  }

  if (input.publishStatus !== "published") {
    reasons.push("не опубликован");
  }

  if (!input.slug?.trim()) {
    reasons.push("нет slug");
  }

  if (!input.categoryName?.trim()) {
    reasons.push("нет категории");
  }

  return reasons;
}

function getCategoryVisibilityReasons(input: Extract<HomepageTargetVisibilityInput, { type: "category" }>) {
  const reasons: string[] = [];

  if (!input.isActive) {
    reasons.push("категория неактивна");
  }

  if (!input.slug?.trim()) {
    reasons.push("нет slug");
  }

  if (!input.isVisibleInMenu) {
    reasons.push("не в меню");
  }

  return reasons;
}
