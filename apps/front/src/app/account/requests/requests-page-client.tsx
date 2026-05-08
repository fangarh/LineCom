"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { RequestList } from "@/components/account/request-list";
import { useAuth } from "@/components/auth/auth-provider";
import { getMe } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { getCustomerRequests, type CustomerRequestListItem } from "@/lib/api/requests";
import { routes } from "@/lib/routes";

export function RequestsPageClient() {
  const router = useRouter();
  const { setSession } = useAuth();
  const [requests, setRequests] = useState<CustomerRequestListItem[]>([]);
  const [status, setStatus] = useState("all");
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  const redirectToLogin = useCallback(() => {
    router.push(routes.login(routes.accountRequests()));
  }, [router]);

  useEffect(() => {
    let isActive = true;

    async function loadRequests() {
      setIsLoading(true);
      setPageError(null);

      try {
        const session = await getMe();
        if (!isActive) return;
        setSession(session);

        const response = await getCustomerRequests({ status: status === "all" ? undefined : status });
        if (!isActive) return;
        setRequests(response.items);
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

    loadRequests();

    return () => {
      isActive = false;
    };
  }, [redirectToLogin, setSession, status]);

  return (
    <div className="account-page">
      <section className="account-intro" aria-labelledby="requests-title">
        <div>
          <p className="eyebrow">Личный кабинет</p>
          <h1 id="requests-title">Мои заявки</h1>
          <p className="lead-text">Отслеживайте отправленные обращения и открывайте карточки по публичному номеру.</p>
        </div>
      </section>

      {isLoading ? <p className="empty-state">Загружаем заявки...</p> : null}
      {pageError ? (
        <p className="form-alert" role="alert">
          {pageError}
        </p>
      ) : null}

      {!isLoading && !pageError ? <RequestList requests={requests} status={status} onStatusChange={setStatus} /> : null}
    </div>
  );
}
