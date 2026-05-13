"use client";

import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from "react";
import { getMe, logout, type AuthSession, type CurrentUser } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";

type AuthContextValue = {
  user: CurrentUser | null;
  csrfToken: string | null;
  isRestoringSession: boolean;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
  restoreSession: () => Promise<void>;
  logoutSession: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [csrfToken, setCsrfToken] = useState<string | null>(null);
  const [isRestoringSession, setIsRestoringSession] = useState(false);
  const authGenerationRef = useRef(0);
  const restoreRequestIdRef = useRef(0);

  const applySession = useCallback((session: AuthSession) => {
    setUser(session.user);
    setCsrfToken(session.csrfToken);
  }, []);

  const applyAnonymousSession = useCallback(() => {
    setUser(null);
    setCsrfToken(null);
  }, []);

  const invalidateRestore = useCallback(() => {
    authGenerationRef.current += 1;
  }, []);

  const setSession = useCallback((session: AuthSession) => {
    invalidateRestore();
    applySession(session);
  }, [applySession, invalidateRestore]);

  const clearSession = useCallback(() => {
    invalidateRestore();
    applyAnonymousSession();
  }, [applyAnonymousSession, invalidateRestore]);

  const restoreSession = useCallback(async () => {
    const requestId = restoreRequestIdRef.current + 1;
    restoreRequestIdRef.current = requestId;
    const authGeneration = authGenerationRef.current;
    setIsRestoringSession(true);

    try {
      const session = await getMe();
      if (requestId === restoreRequestIdRef.current && authGeneration === authGenerationRef.current) {
        applySession(session);
      }
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (
        requestId === restoreRequestIdRef.current &&
        authGeneration === authGenerationRef.current &&
        (apiError.code === "auth.unauthorized" || apiError.code === "auth.user_inactive")
      ) {
        applyAnonymousSession();
      }
    } finally {
      if (requestId === restoreRequestIdRef.current) {
        setIsRestoringSession(false);
      }
    }
  }, [applyAnonymousSession, applySession]);

  const logoutSession = useCallback(async () => {
    invalidateRestore();

    try {
      await logout(csrfToken);
      applyAnonymousSession();
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (apiError.code === "auth.unauthorized" || apiError.code === "auth.user_inactive") {
        applyAnonymousSession();
        return;
      }

      throw error;
    }
  }, [applyAnonymousSession, csrfToken, invalidateRestore]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      csrfToken,
      isRestoringSession,
      setSession,
      clearSession,
      restoreSession,
      logoutSession,
    }),
    [clearSession, csrfToken, isRestoringSession, logoutSession, restoreSession, setSession, user],
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
