import type { Metadata } from "next";
import { Suspense } from "react";
import { redirect } from "next/navigation";
import { noindexPageMetadata } from "@/lib/seo/metadata";
import { routes } from "@/lib/routes";
import { siteFeatures } from "@/lib/site-features";
import { RegisterPageClient } from "./register-page-client";

export const metadata: Metadata = noindexPageMetadata("Регистрация LineCom");

export default function RegisterPage() {
  if (!siteFeatures.customerRegistration) {
    redirect(routes.login());
  }

  return (
    <Suspense fallback={<div className="auth-page" />}>
      <RegisterPageClient />
    </Suspense>
  );
}
