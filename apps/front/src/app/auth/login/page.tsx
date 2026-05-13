import type { Metadata } from "next";
import { Suspense } from "react";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { LoginPageClient } from "./login-page-client";

export const metadata: Metadata = noindexPageMetadata("Вход в LineCom");

export default function LoginPage() {
  return (
    <Suspense fallback={<div className="auth-page" />}>
      <LoginPageClient />
    </Suspense>
  );
}
