"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AdminCatalogShell } from "@/components/admin/catalog/admin-catalog-shell";
import { useAuth } from "@/components/auth/auth-provider";
import { getMe } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { routes } from "@/lib/routes";

export function CatalogPageClient() {
  const router = useRouter();
  const { setSession } = useAuth();
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [isForbidden, setIsForbidden] = useState(false);
  const [canManageCatalog, setCanManageCatalog] = useState(false);
  const [csrfToken, setCsrfToken] = useState<string | null>(null);

  const redirectToLogin = useCallback(() => {
    router.push(routes.login(routes.adminCatalog()));
  }, [router]);

  useEffect(() => {
    let isActive = true;

    async function loadSession() {
      setIsInitialLoading(true);
      setPageError(null);
      setIsForbidden(false);
      setCanManageCatalog(false);
      setCsrfToken(null);

      try {
        const session = await getMe();
        if (!isActive) return;
        setSession(session);
        setCsrfToken(session.csrfToken);

        if (session.user.role !== "seller" && session.user.role !== "admin") {
          setIsForbidden(true);
          return;
        }

        setCanManageCatalog(true);
      } catch (error) {
        const apiError = normalizeApiError(error);

        if (apiError.code === "auth.unauthorized") {
          if (!isActive) return;
          redirectToLogin();
          return;
        }

        if (apiError.code === "auth.forbidden") {
          if (isActive) {
            setIsForbidden(true);
          }
          return;
        }

        if (isActive) {
          setPageError(apiError.message);
        }
      } finally {
        if (isActive) {
          setIsInitialLoading(false);
        }
      }
    }

    loadSession();

    return () => {
      isActive = false;
    };
  }, [redirectToLogin, setSession]);

  return (
    <div className="account-page admin-catalog-page">
      {isInitialLoading ? <p className="empty-state">Загружаем управление каталогом...</p> : null}

      {pageError ? (
        <p className="form-alert" role="alert">
          {pageError}
        </p>
      ) : null}

      {isForbidden ? (
        <p className="form-alert" role="alert">
          У вас нет доступа к управлению каталогом.
        </p>
      ) : null}

      {!isInitialLoading && !pageError && !isForbidden && canManageCatalog ? (
        <AdminCatalogShell csrfToken={csrfToken} />
      ) : null}
    </div>
  );
}
