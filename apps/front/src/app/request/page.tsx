"use client";

import { useRouter } from "next/navigation";
import { RequestDraftView } from "@/components/request/request-draft-view";
import { routes } from "@/lib/routes";

export default function RequestPage() {
  const router = useRouter();

  return <RequestDraftView onSubmit={() => router.push(routes.login(routes.request()))} />;
}
