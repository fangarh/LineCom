"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { OrganizationForm } from "@/components/account/organization-form";
import { PasswordForm } from "@/components/account/password-form";
import { ProfileForm } from "@/components/account/profile-form";
import { useAuth } from "@/components/auth/auth-provider";
import {
  changePassword,
  getAccountProfile,
  updateAccountProfile,
  upsertOrganization,
  type AccountProfile,
  type ChangePasswordPayload,
  type UpdateAccountProfilePayload,
  type UpsertOrganizationPayload,
} from "@/lib/api/account";
import { getMe } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { routes } from "@/lib/routes";

export function ProfilePageClient() {
  const router = useRouter();
  const { csrfToken, setSession } = useAuth();
  const [profile, setProfile] = useState<AccountProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileSuccess, setProfileSuccess] = useState<string | null>(null);
  const [organizationError, setOrganizationError] = useState<string | null>(null);
  const [organizationSuccess, setOrganizationSuccess] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  const redirectToLogin = useCallback(() => {
    router.push(routes.login(routes.accountProfile()));
  }, [router]);

  useEffect(() => {
    let isActive = true;

    async function loadProfile() {
      setIsLoading(true);
      setPageError(null);

      try {
        const session = await getMe();
        if (!isActive) return;
        setSession(session);

        const accountProfile = await getAccountProfile();
        if (!isActive) return;
        setProfile(accountProfile);
      } catch (error) {
        const apiError = normalizeApiError(error);
        if (apiError.code === "auth.unauthorized") {
          redirectToLogin();
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

    loadProfile();

    return () => {
      isActive = false;
    };
  }, [redirectToLogin, setSession]);

  const profileInitialValue = useMemo<UpdateAccountProfilePayload>(
    () => ({
      name: profile?.user.name ?? "",
      email: profile?.user.email ?? null,
      phone: profile?.user.phone ?? null,
    }),
    [profile?.user.email, profile?.user.name, profile?.user.phone],
  );

  async function handleProfileSubmit(payload: UpdateAccountProfilePayload) {
    setProfileError(null);
    setProfileSuccess(null);

    if (!csrfToken) {
      setProfileError("Сессия не готова. Войдите в аккаунт повторно.");
      return;
    }

    try {
      const user = await updateAccountProfile(payload, csrfToken);
      setSession({ user, csrfToken });
      setProfile((current) => (current ? { ...current, user } : { user, organization: null }));
      setProfileSuccess("Профиль сохранен.");
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (apiError.code === "auth.unauthorized") {
        redirectToLogin();
        return;
      }

      setProfileError(apiError.message);
    }
  }

  async function handleOrganizationSubmit(payload: UpsertOrganizationPayload) {
    setOrganizationError(null);
    setOrganizationSuccess(null);

    if (!csrfToken) {
      setOrganizationError("Сессия не готова. Войдите в аккаунт повторно.");
      return;
    }

    try {
      const organization = await upsertOrganization(payload, csrfToken);
      setProfile((current) => (current ? { ...current, organization } : current));
      setOrganizationSuccess("Организация сохранена.");
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (apiError.code === "auth.unauthorized") {
        redirectToLogin();
        return;
      }

      setOrganizationError(apiError.message);
    }
  }

  async function handlePasswordSubmit(payload: ChangePasswordPayload) {
    setPasswordError(null);

    if (!csrfToken) {
      setPasswordError("Сессия не готова. Войдите в аккаунт повторно.");
      throw new Error("CSRF token is not ready.");
    }

    try {
      await changePassword(payload, csrfToken);
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (apiError.code === "auth.unauthorized") {
        redirectToLogin();
        throw error;
      }

      setPasswordError(apiError.message);
      throw error;
    }
  }

  return (
    <div className="account-page">
      <section className="account-intro" aria-labelledby="profile-title">
        <div>
          <p className="eyebrow">Личный кабинет</p>
          <h1 id="profile-title">Профиль и организация</h1>
          <p className="lead-text">
            Эти данные помогут менеджеру быстрее обработать заявку. Организация необязательна и не создается при регистрации.
          </p>
        </div>
      </section>

      {isLoading ? <p className="empty-state">Загружаем профиль...</p> : null}
      {pageError ? (
        <p className="form-alert" role="alert">
          {pageError}
        </p>
      ) : null}

      {!isLoading && profile ? (
        <div className="account-grid">
          <section className="account-section" aria-labelledby="profile-form-title">
            <h2 id="profile-form-title">Контакты</h2>
            <ProfileForm
              key={`${profile.user.id}:${profile.user.name}:${profile.user.email ?? ""}:${profile.user.phone ?? ""}`}
              initialValue={profileInitialValue}
              onSubmit={handleProfileSubmit}
              errorMessage={profileError}
              successMessage={profileSuccess}
            />
          </section>

          <section className="account-section" aria-labelledby="organization-form-title">
            <h2 id="organization-form-title">Организация</h2>
            <OrganizationForm
              key={[
                profile.organization?.name ?? "",
                profile.organization?.inn ?? "",
                profile.organization?.contactPerson ?? "",
                profile.organization?.phone ?? "",
                profile.organization?.email ?? "",
                profile.organization?.comment ?? "",
              ].join(":")}
              initialValue={profile.organization}
              onSubmit={handleOrganizationSubmit}
              errorMessage={organizationError}
              successMessage={organizationSuccess}
            />
          </section>

          <section className="account-section" aria-labelledby="password-form-title">
            <h2 id="password-form-title">Пароль</h2>
            <PasswordForm onSubmit={handlePasswordSubmit} errorMessage={passwordError} />
          </section>
        </div>
      ) : null}
    </div>
  );
}
