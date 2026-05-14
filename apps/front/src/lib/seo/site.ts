const DEFAULT_PUBLIC_SITE_ORIGIN = "http://127.0.0.1:3000";
const PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR =
  "LINECOM_PUBLIC_SITE_ORIGIN must be an absolute non-localhost URL in production, e.g. https://line-com.ru";

export function normalizeSiteOrigin(value: string | null | undefined, environment = process.env.NODE_ENV) {
  const trimmed = value?.trim();
  if (!trimmed) {
    if (environment === "production") {
      throw new Error(PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR);
    }

    return DEFAULT_PUBLIC_SITE_ORIGIN;
  }

  try {
    const parsed = new URL(trimmed);
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
      if (environment === "production") {
        throw new Error(PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR);
      }

      return DEFAULT_PUBLIC_SITE_ORIGIN;
    }

    if (environment === "production" && isLocalhostOrigin(parsed.hostname)) {
      throw new Error(PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR);
    }

    if (environment === "production" && !parsed.hostname) {
      throw new Error(PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR);
    }

    return parsed.origin;
  } catch (error) {
    if (environment === "production") {
      if (error instanceof Error && error.message === PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR) {
        throw error;
      }

      throw new Error(PUBLIC_SITE_ORIGIN_PRODUCTION_ERROR);
    }

    return DEFAULT_PUBLIC_SITE_ORIGIN;
  }
}

function isLocalhostOrigin(hostname: string) {
  const normalized = hostname.trim().toLowerCase().replace(/^\[/, "").replace(/\]$/, "");
  return normalized === "localhost" || normalized === "127.0.0.1" || normalized === "::1";
}

export function getPublicSiteOrigin() {
  return normalizeSiteOrigin(process.env.LINECOM_PUBLIC_SITE_ORIGIN);
}

export function siteMetadataBase() {
  return new URL(getPublicSiteOrigin());
}

export function absoluteSiteUrl(path: string) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${getPublicSiteOrigin()}${normalizedPath}`;
}
