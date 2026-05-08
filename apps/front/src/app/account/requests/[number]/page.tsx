import { RequestDetailPageClient } from "./request-detail-page-client";

type AccountRequestDetailPageProps = {
  params: Promise<{
    number: string;
  }>;
};

export default async function AccountRequestDetailPage({ params }: AccountRequestDetailPageProps) {
  const { number } = await params;

  return <RequestDetailPageClient number={decodeURIComponent(number)} />;
}
