import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

describe("llms.txt", () => {
  it("publishes a concise LLM crawler map with company facts and key sections", () => {
    const llmsText = readFileSync(path.join(process.cwd(), "public", "llms.txt"), "utf8");

    expect(llmsText).toContain("# LineCom");
    expect(llmsText).toContain("https://line-com.ru/catalog");
    expect(llmsText).toContain("https://line-com.ru/catalog/twisted-pair-cable");
    expect(llmsText).toContain("https://line-com.ru/contacts");
    expect(llmsText).not.toContain("https://line-com.ru/about");
    expect(llmsText).toContain("ООО «ЛАЙНКОМ»");
    expect(llmsText).toContain("ИНН: 7801724840");
    expect(llmsText).toContain("Sitemap: https://line-com.ru/sitemap.xml");
  });
});
