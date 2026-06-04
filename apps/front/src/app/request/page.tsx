import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { routes } from "@/lib/routes";
import { siteFeatures } from "@/lib/site-features";
import { RequestPageClient } from "./request-page-client";

export const metadata: Metadata = {
  ...noindexPageMetadata("Заявка LineCom"),
  alternates: {
    canonical: "/request",
  },
};

export default function RequestPage() {
  if (!siteFeatures.customerRequests) {
    redirect(routes.catalog());
  }

  return <RequestPageClient />;
}
