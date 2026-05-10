import { AdminRequestDetailPageClient } from "./request-detail-page-client";

type AdminRequestDetailPageProps = {
  params: Promise<{
    number: string;
  }>;
};

export default async function AdminRequestDetailPage({ params }: AdminRequestDetailPageProps) {
  const { number } = await params;

  return <AdminRequestDetailPageClient number={decodeURIComponent(number)} />;
}
