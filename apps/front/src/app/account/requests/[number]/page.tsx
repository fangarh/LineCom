import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { RequestDetailPageClient } from "./request-detail-page-client";

export const metadata: Metadata = noindexPageMetadata("Заявка LineCom");

type AccountRequestDetailPageProps = {
  params: Promise<{
    number: string;
  }>;
};

export default async function AccountRequestDetailPage({ params }: AccountRequestDetailPageProps) {
  const { number } = await params;

  return <RequestDetailPageClient number={decodeURIComponent(number)} />;
}
