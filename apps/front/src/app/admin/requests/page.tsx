import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { RequestsPageClient } from "./requests-page-client";

export const metadata: Metadata = noindexPageMetadata("Админка заявок LineCom");

export default function AdminRequestsPage() {
  return <RequestsPageClient />;
}
