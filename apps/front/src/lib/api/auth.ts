import { apiJson } from "./http";

export type CurrentUser = {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  role: string;
};

export type AuthSession = {
  user: CurrentUser;
  csrfToken: string;
};

export type RegisterPayload = {
  name: string;
  email: string | null;
  phone: string | null;
  password: string;
};

export type LoginPayload = {
  login: string;
  password: string;
};

export function register(payload: RegisterPayload) {
  return apiJson<AuthSession>("/api/auth/register", {
    method: "POST",
    body: payload,
  });
}

export function login(payload: LoginPayload) {
  return apiJson<AuthSession>("/api/auth/login", {
    method: "POST",
    body: payload,
  });
}

export function getMe() {
  return apiJson<AuthSession>("/api/auth/me", {
    cache: "no-store",
  });
}

export function logout(csrfToken: string | null) {
  return apiJson<void>("/api/auth/logout", {
    method: "POST",
    csrfToken,
  });
}
