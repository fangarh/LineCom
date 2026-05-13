import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { RequestsPageClient } from "./requests-page-client";

export const metadata: Metadata = noindexPageMetadata("Мои заявки LineCom");

export default function AccountRequestsPage() {
  return <RequestsPageClient />;
}
