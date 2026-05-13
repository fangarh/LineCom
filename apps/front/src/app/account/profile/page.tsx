import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { ProfilePageClient } from "./profile-page-client";

export const metadata: Metadata = noindexPageMetadata("Профиль LineCom");

export default function AccountProfilePage() {
  return <ProfilePageClient />;
}
