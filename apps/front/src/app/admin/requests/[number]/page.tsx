import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { AdminRequestDetailPageClient } from "./request-detail-page-client";

export const metadata: Metadata = noindexPageMetadata("Админка заявки LineCom");

type AdminRequestDetailPageProps = {
  params: Promise<{
    number: string;
  }>;
};

export default async function AdminRequestDetailPage({ params }: AdminRequestDetailPageProps) {
  const { number } = await params;

  return <AdminRequestDetailPageClient number={decodeURIComponent(number)} />;
}
