"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { getMe, type AuthSession, type CurrentUser } from "@/lib/api/auth";
import { ApiClientError } from "@/lib/api/errors";

type AuthStatus = "idle" | "restoring" | "authenticated" | "anonymous";

type AuthContextValue = {
  user: CurrentUser | null;
  csrfToken: string | null;
  status: AuthStatus;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
  restoreSession: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

type AuthProviderProps = {
  children: ReactNode;
  initialSession?: AuthSession | null;
};

export function AuthProvider({ children, initialSession }: AuthProviderProps) {
  const hasInitialSession = initialSession !== undefined;
  const [user, setUser] = useState<CurrentUser | null>(initialSession?.user ?? null);
  const [csrfToken, setCsrfToken] = useState<string | null>(initialSession?.csrfToken ?? null);
  const [status, setStatus] = useState<AuthStatus>(
    initialSession ? "authenticated" : hasInitialSession ? "anonymous" : "restoring",
  );
  const sessionRef = useRef<AuthSession | null>(initialSession ?? null);
  const authVersionRef = useRef(0);
  const restoreRequestRef = useRef(0);

  const setSession = useCallback((session: AuthSession) => {
    authVersionRef.current += 1;
    sessionRef.current = session;
    setUser(session.user);
    setCsrfToken(session.csrfToken);
    setStatus("authenticated");
  }, []);

  const clearSession = useCallback(() => {
    authVersionRef.current += 1;
    sessionRef.current = null;
    setUser(null);
    setCsrfToken(null);
    setStatus("anonymous");
  }, []);

  const restoreSession = useCallback(async () => {
    const restoreRequest = restoreRequestRef.current + 1;
    restoreRequestRef.current = restoreRequest;
    const startedAuthVersion = authVersionRef.current;

    setStatus("restoring");

    try {
      const session = await getMe();

      if (restoreRequestRef.current !== restoreRequest || authVersionRef.current !== startedAuthVersion) {
        return;
      }

      setSession(session);
    } catch (error) {
      if (restoreRequestRef.current !== restoreRequest || authVersionRef.current !== startedAuthVersion) {
        return;
      }

      if (error instanceof ApiClientError && error.code === "auth.unauthorized") {
        clearSession();
        return;
      }

      clearSession();
    }
  }, [clearSession, setSession]);

  useEffect(() => {
    if (hasInitialSession) {
      return;
    }

    const restoreTimer = window.setTimeout(() => {
      if (!sessionRef.current) {
        void restoreSession();
      }
    }, 0);

    return () => {
      window.clearTimeout(restoreTimer);
    };
  }, [hasInitialSession, restoreSession]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      csrfToken,
      status,
      setSession,
      clearSession,
      restoreSession,
    }),
    [clearSession, csrfToken, restoreSession, setSession, status, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return value;
}
