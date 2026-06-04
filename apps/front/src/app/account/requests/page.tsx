import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { RequestsPageClient } from "./requests-page-client";

export const metadata: Metadata = noindexPageMetadata("История заказов LineCom");

export default function AccountRequestsPage() {
  return <RequestsPageClient />;
}
