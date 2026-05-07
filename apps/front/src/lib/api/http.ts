import { ApiClientError, type ApiErrorResponse, isApiErrorResponse } from "./errors";

type JsonRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  csrfToken?: string | null;
  cache?: RequestCache;
  next?: NextFetchRequestConfig;
};

export async function apiJson<T>(path: string, options: JsonRequestOptions = {}): Promise<T> {
  const headers = new Headers();
  headers.set("Accept", "application/json");

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.csrfToken) {
    headers.set("X-CSRF-Token", options.csrfToken);
  }

  const response = await fetch(path, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    credentials: "include",
    cache: options.cache,
    next: options.next,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const payload = text ? (JSON.parse(text) as unknown) : null;

  if (!response.ok) {
    const apiError: ApiErrorResponse = isApiErrorResponse(payload)
      ? payload
      : { code: "internal_error", message: "Внутренняя ошибка сервера." };
    throw new ApiClientError(response.status, apiError);
  }

  return payload as T;
}
