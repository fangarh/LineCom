export type ApiErrorResponse = {
  code: string;
  message: string;
};

export type ApiClientErrorDiagnostics = {
  readonly reason: "empty_body" | "malformed_json";
  readonly status: number;
  readonly body?: string;
  readonly parseError?: string;
};

export const invalidApiResponseError: ApiErrorResponse = {
  code: "transport.invalid_response",
  message: "Не удалось обработать ответ сервера. Попробуйте позже.",
};

export class ApiClientError extends Error {
  readonly status: number;
  readonly code: string;
  readonly diagnostics?: ApiClientErrorDiagnostics;

  constructor(status: number, error: ApiErrorResponse, diagnostics?: ApiClientErrorDiagnostics) {
    super(error.message);
    this.name = "ApiClientError";
    this.status = status;
    this.code = error.code;
    this.diagnostics = diagnostics;
  }
}

export function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return typeof candidate.code === "string" && typeof candidate.message === "string";
}

export function normalizeApiError(error: unknown): ApiErrorResponse {
  if (error instanceof ApiClientError) {
    return { code: error.code, message: error.message };
  }

  if (isApiErrorResponse(error)) {
    return error;
  }

  return {
    code: "internal_error",
    message: "Внутренняя ошибка сервера.",
  };
}
