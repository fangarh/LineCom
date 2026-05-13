import type { Metadata } from "next";
import { Suspense } from "react";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { RegisterPageClient } from "./register-page-client";

export const metadata: Metadata = noindexPageMetadata("Регистрация LineCom");

export default function RegisterPage() {
  return (
    <Suspense fallback={<div className="auth-page" />}>
      <RegisterPageClient />
    </Suspense>
  );
}
