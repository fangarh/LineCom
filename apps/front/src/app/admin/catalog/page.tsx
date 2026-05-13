import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { CatalogPageClient } from "./catalog-page-client";

export const metadata: Metadata = noindexPageMetadata("Админка каталога LineCom");

export default function AdminCatalogPage() {
  return <CatalogPageClient />;
}
