"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { AdminRequestList, type AdminRequestListFilters } from "@/components/admin/admin-request-list";
import { AdminRequestPreviewDrawer } from "@/components/admin/admin-request-preview-drawer";
import { useAuth } from "@/components/auth/auth-provider";
import {
  getAdminRequest,
  getAdminRequests,
  type AdminRequestDetail,
  type AdminRequestListItem,
} from "@/lib/api/admin-requests";
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
  const [previewNumber, setPreviewNumber] = useState<string | null>(null);
  const [previewRequest, setPreviewRequest] = useState<AdminRequestDetail | null>(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const previewRequestIdRef = useRef(0);

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

  const handlePreviewRequest = useCallback(async (number: string) => {
    const requestId = previewRequestIdRef.current + 1;
    previewRequestIdRef.current = requestId;
    setPreviewNumber(number);
    setPreviewRequest(null);
    setPreviewError(null);
    setIsPreviewLoading(true);

    try {
      const response = await getAdminRequest(number);
      if (previewRequestIdRef.current !== requestId) return;
      setPreviewRequest(response);
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (previewRequestIdRef.current !== requestId) return;
      setPreviewError(apiError.message);
    } finally {
      if (previewRequestIdRef.current === requestId) {
        setIsPreviewLoading(false);
      }
    }
  }, []);

  const closePreview = useCallback(() => {
    previewRequestIdRef.current += 1;
    setPreviewNumber(null);
    setPreviewRequest(null);
    setPreviewError(null);
    setIsPreviewLoading(false);
  }, []);

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
          <AdminRequestList
            requests={requests}
            filters={filters}
            onFiltersChange={setFilters}
            onPreviewRequest={handlePreviewRequest}
          />
        </>
      ) : null}

      <AdminRequestPreviewDrawer
        request={previewRequest}
        isOpen={previewNumber !== null}
        isLoading={isPreviewLoading}
        error={previewError}
        onClose={closePreview}
      />
    </div>
  );
}

function toOptionalFilter(value: string): string | undefined {
  const trimmed = value.trim();
  return trimmed ? trimmed : undefined;
}
