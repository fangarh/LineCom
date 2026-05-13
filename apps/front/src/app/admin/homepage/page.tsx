import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { HomepagePageClient } from "./homepage-page-client";

export const metadata: Metadata = noindexPageMetadata("Админка главной LineCom");

export default function AdminHomepagePage() {
  return <HomepagePageClient />;
}
