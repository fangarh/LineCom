import { describe, expect, it } from "vitest";
import { generateSlug } from "./slug";

describe("generateSlug", () => {
  it("transliterates Russian catalog names into URL slugs", () => {
    expect(generateSlug("Кабель ВВГнг 3x2.5")).toBe("kabel-vvgng-3x2-5");
    expect(generateSlug("  Муфта---кабельная 1кВ  ")).toBe("mufta-kabelnaya-1kv");
    expect(generateSlug("LC/UPC адаптер")).toBe("lc-upc-adapter");
    expect(generateSlug("!!!")).toBe("");
  });
});
