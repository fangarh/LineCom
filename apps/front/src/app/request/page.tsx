import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { RequestPageClient } from "./request-page-client";

export const metadata: Metadata = {
  ...noindexPageMetadata("Заявка LineCom"),
  alternates: {
    canonical: "/request",
  },
};

export default function RequestPage() {
  return <RequestPageClient />;
}
