"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { AdminRequestList, type AdminRequestListFilters } from "@/components/admin/admin-request-list";
import { useAuth } from "@/components/auth/auth-provider";
import { getAdminRequests, type AdminRequestListItem } from "@/lib/api/admin-requests";
import { getMe } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { routes } from "@/lib/routes";

const initialFilters: AdminRequestListFilters = {
  status: "all",
  number: "",
  contact: "",
  organization: "",
};

export function RequestsPageClient() {
  const router = useRouter();
  const { setSession } = useAuth();
  const hasLoadedOnceRef = useRef(false);
  const [requests, setRequests] = useState<AdminRequestListItem[]>([]);
  const [filters, setFilters] = useState<AdminRequestListFilters>(initialFilters);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [isForbidden, setIsForbidden] = useState(false);

  const redirectToLogin = useCallback(() => {
    router.push(routes.login(routes.adminRequests()));
  }, [router]);

  useEffect(() => {
    let isActive = true;

    async function loadRequests() {
      if (hasLoadedOnceRef.current) {
        setIsRefreshing(true);
      } else {
        setIsInitialLoading(true);
      }
      setPageError(null);
      setIsForbidden(false);

      try {
        const session = await getMe();
        if (!isActive) return;
        setSession(session);

        if (session.user.role !== "seller" && session.user.role !== "admin") {
          setRequests([]);
          setIsForbidden(true);
          return;
        }

        const response = await getAdminRequests({
          status: filters.status === "all" ? undefined : filters.status,
          number: toOptionalFilter(filters.number),
          contact: toOptionalFilter(filters.contact),
          organization: toOptionalFilter(filters.organization),
        });
        if (!isActive) return;
        setRequests(response.items);
      } catch (error) {
        const apiError = normalizeApiError(error);

        if (apiError.code === "auth.unauthorized") {
          if (!isActive) return;
          redirectToLogin();
          return;
        }

        if (apiError.code === "auth.forbidden") {
          if (isActive) {
            setRequests([]);
            setIsForbidden(true);
          }
          return;
        }

        if (isActive) {
          setPageError(apiError.message);
        }
      } finally {
        if (isActive) {
          hasLoadedOnceRef.current = true;
          setIsInitialLoading(false);
          setIsRefreshing(false);
        }
      }
    }

    loadRequests();

    return () => {
      isActive = false;
    };
  }, [filters, redirectToLogin, setSession]);

  return (
    <div className="account-page admin-requests-page">
      {isInitialLoading ? <p className="empty-state">Загружаем заявки...</p> : null}

      {pageError ? (
        <p className="form-alert" role="alert">
          {pageError}
        </p>
      ) : null}

      {isForbidden ? (
        <p className="form-alert" role="alert">
          У вас нет доступа к очереди заявок.
        </p>
      ) : null}

      {!isInitialLoading && !pageError && !isForbidden ? (
        <>
          {isRefreshing ? <p className="empty-state">Обновляем список заявок...</p> : null}
          <AdminRequestList requests={requests} filters={filters} onFiltersChange={setFilters} />
        </>
      ) : null}
    </div>
  );
}

function toOptionalFilter(value: string): string | undefined {
  const trimmed = value.trim();
  return trimmed ? trimmed : undefined;
}
