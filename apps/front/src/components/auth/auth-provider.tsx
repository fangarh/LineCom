"use client";

import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
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
  const setSession = useCallback((session: AuthSession) => {
    setUser(session.user);
    setCsrfToken(session.csrfToken);
  }, []);
  const clearSession = useCallback(() => {
    setUser(null);
    setCsrfToken(null);
  }, []);
  const restoreSession = useCallback(async () => {
    setIsRestoringSession(true);

    try {
      const session = await getMe();
      setSession(session);
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (apiError.code === "auth.unauthorized") {
        clearSession();
      }
    } finally {
      setIsRestoringSession(false);
    }
  }, [clearSession, setSession]);
  const logoutSession = useCallback(async () => {
    await logout(csrfToken);
    clearSession();
  }, [clearSession, csrfToken]);

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
