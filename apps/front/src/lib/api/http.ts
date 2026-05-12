import { ApiClientError, type ApiErrorResponse, isApiErrorResponse } from "./errors";

type JsonRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  csrfToken?: string | null;
  cache?: RequestCache;
  next?: NextFetchRequestConfig;
};

type FormRequestOptions = {
  method?: "POST" | "PUT" | "PATCH" | "DELETE";
  body: FormData;
  csrfToken?: string | null;
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

  const response = await fetch(resolveApiPath(path), {
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

export async function apiForm<T>(path: string, options: FormRequestOptions): Promise<T> {
  const headers = new Headers();
  headers.set("Accept", "application/json");

  if (options.csrfToken) {
    headers.set("X-CSRF-Token", options.csrfToken);
  }

  const response = await fetch(resolveApiPath(path), {
    method: options.method ?? "POST",
    headers,
    body: options.body,
    credentials: "include",
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

function resolveApiPath(path: string): string {
  if (/^https?:\/\//.test(path) || typeof window !== "undefined") {
    return path;
  }

  if (path.startsWith("/api/")) {
    const apiOrigin = process.env.LINECOM_API_ORIGIN ?? "http://127.0.0.1:8080";
    return `${apiOrigin}${path}`;
  }

  return path;
}
