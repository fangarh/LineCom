"use client";

import { useCallback, useEffect, useRef, useState, type RefObject } from "react";
import { useRouter } from "next/navigation";
import { AdminRequestDetail } from "@/components/admin/admin-request-detail";
import { useAuth } from "@/components/auth/auth-provider";
import {
  getAdminRequest,
  updateAdminRequestInternalComment,
  updateAdminRequestStatus,
  type AdminRequestDetail as AdminRequestDetailModel,
  type AdminRequestStatusCode,
} from "@/lib/api/admin-requests";
import { getMe } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { routes } from "@/lib/routes";

type AdminRequestDetailPageClientProps = {
  number: string;
};

export function AdminRequestDetailPageClient({ number }: AdminRequestDetailPageClientProps) {
  const router = useRouter();
  const { setSession } = useAuth();
  const [request, setRequest] = useState<AdminRequestDetailModel | null>(null);
  const [csrfToken, setCsrfToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [isForbidden, setIsForbidden] = useState(false);
  const [isStatusSaving, setIsStatusSaving] = useState(false);
  const [isCommentSaving, setIsCommentSaving] = useState(false);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const mountedRef = useRef(false);
  const currentNumberRef = useRef(number);
  const isStatusSavingRef = useRef(false);
  const isCommentSavingRef = useRef(false);
  const statusMutationSeqRef = useRef(0);
  const commentMutationSeqRef = useRef(0);

  useEffect(() => {
    mountedRef.current = true;

    return () => {
      mountedRef.current = false;
      statusMutationSeqRef.current += 1;
      commentMutationSeqRef.current += 1;
    };
  }, []);

  const redirectToLogin = useCallback(() => {
    router.push(routes.login(routes.adminRequest(number)));
  }, [number, router]);

  useEffect(() => {
    let isActive = true;
    currentNumberRef.current = number;
    statusMutationSeqRef.current += 1;
    commentMutationSeqRef.current += 1;
    isStatusSavingRef.current = false;
    isCommentSavingRef.current = false;

    async function loadRequest() {
      setIsStatusSaving(false);
      setIsCommentSaving(false);
      setIsLoading(true);
      setPageError(null);
      setIsForbidden(false);
      setActionMessage(null);
      setActionError(null);

      try {
        const session = await getMe();
        if (!isActive) return;
        setSession(session);
        setCsrfToken(session.csrfToken);

        if (session.user.role !== "seller" && session.user.role !== "admin") {
          setRequest(null);
          setIsForbidden(true);
          return;
        }

        const response = await getAdminRequest(number);
        if (!isActive) return;
        setRequest(response);
      } catch (error) {
        const apiError = normalizeApiError(error);

        if (apiError.code === "auth.unauthorized") {
          if (!isActive) return;
          redirectToLogin();
          return;
        }

        if (apiError.code === "auth.forbidden") {
          if (isActive) {
            setRequest(null);
            setIsForbidden(true);
          }
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

  const saveStatus = useCallback(
    async (status: AdminRequestStatusCode) => {
      if (isStatusSavingRef.current) return;

      if (!csrfToken) {
        setActionError("Сессия не подтверждена. Обновите страницу и войдите снова.");
        return;
      }

      const mutationSeq = statusMutationSeqRef.current + 1;
      statusMutationSeqRef.current = mutationSeq;
      isStatusSavingRef.current = true;
      setIsStatusSaving(true);
      setActionMessage(null);
      setActionError(null);

      try {
        const response = await updateAdminRequestStatus(number, status, csrfToken);
        if (!isCurrentMutation(mountedRef, currentNumberRef, number, statusMutationSeqRef, mutationSeq)) return;
        setRequest(response);
        setActionMessage("Статус сохранен.");
      } catch (error) {
        if (!isCurrentMutation(mountedRef, currentNumberRef, number, statusMutationSeqRef, mutationSeq)) return;
        setActionError(normalizeApiError(error).message);
      } finally {
        if (isCurrentMutation(mountedRef, currentNumberRef, number, statusMutationSeqRef, mutationSeq)) {
          setIsStatusSaving(false);
        }
        if (statusMutationSeqRef.current === mutationSeq) {
          isStatusSavingRef.current = false;
        }
      }
    },
    [csrfToken, number],
  );

  const saveInternalComment = useCallback(
    async (comment: string) => {
      if (isCommentSavingRef.current) return;

      if (!csrfToken) {
        setActionError("Сессия не подтверждена. Обновите страницу и войдите снова.");
        return;
      }

      const mutationSeq = commentMutationSeqRef.current + 1;
      commentMutationSeqRef.current = mutationSeq;
      isCommentSavingRef.current = true;
      setIsCommentSaving(true);
      setActionMessage(null);
      setActionError(null);

      try {
        const normalizedComment = comment.trim() ? comment : "";
        const response = await updateAdminRequestInternalComment(number, normalizedComment, csrfToken);
        if (!isCurrentMutation(mountedRef, currentNumberRef, number, commentMutationSeqRef, mutationSeq)) return;
        setRequest(response);
        setActionMessage("Комментарий сохранен.");
      } catch (error) {
        if (!isCurrentMutation(mountedRef, currentNumberRef, number, commentMutationSeqRef, mutationSeq)) return;
        setActionError(normalizeApiError(error).message);
      } finally {
        if (isCurrentMutation(mountedRef, currentNumberRef, number, commentMutationSeqRef, mutationSeq)) {
          setIsCommentSaving(false);
        }
        if (commentMutationSeqRef.current === mutationSeq) {
          isCommentSavingRef.current = false;
        }
      }
    },
    [csrfToken, number],
  );

  return (
    <div className="account-page admin-request-detail-page">
      <section className="account-intro" aria-labelledby="admin-request-detail-title">
        <div>
          <p className="eyebrow">Админка</p>
          <h1 id="admin-request-detail-title">Карточка заявки</h1>
          <p className="lead-text">{number}</p>
        </div>
      </section>

      {isLoading ? <p className="empty-state">Загружаем заявку...</p> : null}

      {pageError ? (
        <p className="form-alert" role="alert">
          {pageError}
        </p>
      ) : null}

      {isForbidden ? (
        <p className="form-alert" role="alert">
          У вас нет доступа к карточке заявки.
        </p>
      ) : null}

      {!isLoading && !pageError && !isForbidden && request ? (
        <AdminRequestDetail
          key={`${request.number}:${request.status.code}:${request.internalComment ?? ""}:${request.updatedAt}`}
          request={request}
          onStatusSave={saveStatus}
          onInternalCommentSave={saveInternalComment}
          isStatusSaving={isStatusSaving}
          isCommentSaving={isCommentSaving}
          canSave={Boolean(csrfToken)}
          actionMessage={actionMessage}
          actionError={actionError}
        />
      ) : null}
    </div>
  );
}

function isCurrentMutation(
  mountedRef: RefObject<boolean>,
  currentNumberRef: RefObject<string>,
  number: string,
  sequenceRef: RefObject<number>,
  sequence: number,
) {
  return mountedRef.current && currentNumberRef.current === number && sequenceRef.current === sequence;
}
