import { Suspense } from "react";
import { RegisterPageClient } from "./register-page-client";

export default function RegisterPage() {
  return (
    <Suspense fallback={<div className="auth-page" />}>
      <RegisterPageClient />
    </Suspense>
  );
}
