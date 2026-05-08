"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { RequestDetail } from "@/components/account/request-detail";
import { useAuth } from "@/components/auth/auth-provider";
import { getMe } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { getCustomerRequest, type CustomerRequestDetail } from "@/lib/api/requests";
import { routes } from "@/lib/routes";

type RequestDetailPageClientProps = {
  number: string;
};

export function RequestDetailPageClient({ number }: RequestDetailPageClientProps) {
  const router = useRouter();
  const { setSession } = useAuth();
  const [request, setRequest] = useState<CustomerRequestDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  const redirectToLogin = useCallback(() => {
    router.push(routes.login(`${routes.accountRequests()}/${number}`));
  }, [number, router]);

  useEffect(() => {
    let isActive = true;

    async function loadRequest() {
      setIsLoading(true);
      setPageError(null);

      try {
        const session = await getMe();
        if (!isActive) return;
        setSession(session);

        const response = await getCustomerRequest(number);
        if (!isActive) return;
        setRequest(response);
      } catch (error) {
        const apiError = normalizeApiError(error);

        if (apiError.code === "auth.unauthorized") {
          redirectToLogin();
          return;
        }

        if (isActive) {
          setPageError(apiError.message);
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadRequest();

    return () => {
      isActive = false;
    };
  }, [number, redirectToLogin, setSession]);

  return (
    <div className="account-page">
      <section className="account-intro" aria-labelledby="request-detail-title">
        <div>
          <p className="eyebrow">Личный кабинет</p>
          <h1 id="request-detail-title">Карточка заявки</h1>
          <p className="lead-text">{number}</p>
        </div>
      </section>

      {isLoading ? <p className="empty-state">Загружаем заявку...</p> : null}
      {pageError ? (
        <p className="form-alert" role="alert">
          {pageError}
        </p>
      ) : null}

      {!isLoading && !pageError && request ? <RequestDetail request={request} /> : null}
    </div>
  );
}
