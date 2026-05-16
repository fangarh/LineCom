"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/components/auth/auth-provider";
import { RequestDraftView } from "@/components/request/request-draft-view";
import { useRequestDraft } from "@/components/request/request-draft-provider";
import { getMe } from "@/lib/api/auth";
import { ApiClientError, normalizeApiError } from "@/lib/api/errors";
import { createCustomerRequest, type CreateCustomerRequestPayload } from "@/lib/api/requests";
import { routes } from "@/lib/routes";
import { isDraftEmpty } from "@/lib/request-draft/selectors";
import type { RequestDraftState } from "@/lib/request-draft/types";

function buildCreateRequestPayload(state: RequestDraftState): CreateCustomerRequestPayload {
  return {
    source: "cart",
    customerComment: state.customerComment || null,
    items: state.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      customerComment: item.customerComment || null,
    })),
  };
}

export function RequestPageClient() {
  const router = useRouter();
  const auth = useAuth();
  const { state, dispatch } = useRequestDraft();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleSubmit() {
    setErrorMessage(null);

    if (isDraftEmpty(state)) {
      return;
    }

    if (!auth.user || !auth.csrfToken) {
      router.push(routes.login(routes.request()));
      return;
    }

    const payload = buildCreateRequestPayload(state);

    try {
      const request = await createCustomerRequest(payload, auth.csrfToken);

      dispatch({ type: "clear" });
      router.push(routes.accountRequest(request.number));
    } catch (error) {
      if (error instanceof ApiClientError && error.code === "auth.forbidden") {
        try {
          const refreshedSession = await getMe();
          auth.setSession(refreshedSession);
          const request = await createCustomerRequest(payload, refreshedSession.csrfToken);

          dispatch({ type: "clear" });
          router.push(routes.accountRequest(request.number));
          return;
        } catch (retryError) {
          const retryApiError = normalizeApiError(retryError);
          setErrorMessage(retryApiError.message);
          return;
        }
      }

      const apiError = normalizeApiError(error);

      if (apiError.code === "auth.unauthorized") {
        router.push(routes.login(routes.request()));
        return;
      }

      setErrorMessage(apiError.message);
    }
  }

  return <RequestDraftView errorMessage={errorMessage} onSubmit={handleSubmit} />;
}
