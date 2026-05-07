"use client";

import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import type { AuthSession, CurrentUser } from "@/lib/api/auth";

type AuthContextValue = {
  user: CurrentUser | null;
  csrfToken: string | null;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [csrfToken, setCsrfToken] = useState<string | null>(null);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      csrfToken,
      setSession: (session) => {
        setUser(session.user);
        setCsrfToken(session.csrfToken);
      },
      clearSession: () => {
        setUser(null);
        setCsrfToken(null);
      },
    }),
    [csrfToken, user],
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
